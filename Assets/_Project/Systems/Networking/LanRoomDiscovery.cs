using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace PBJ.Networking
{
    // M3 LAN discovery (CLIENT side): passively listens on the shared UDP
    // discovery port for LanRoomBroadcaster announcements, decodes each
    // datagram with the shared LanDiscoveryPacket contract, and keeps the
    // rooms whose code matches the requested one (RoomCode.Matches,
    // case-insensitive). LAN ONLY: one bound UDP socket on the LAN - no
    // relay, no UGS, no internet, and no outgoing traffic (hosts advertise,
    // we listen).
    //
    // Multiple PBJ rooms on one LAN are supported: every announcement is
    // decoded and matched independently; results are keyed by room code (the
    // room's unique identity per the discovery contract) and each entry
    // carries its own host address + advertised game port, so a later join
    // step can connect directly (DiscoveredRoom.ConnectEndPoint ->
    // NetworkSession.StartClient on that address/port).
    //
    // All socket work runs on a dedicated background thread; the main thread
    // only starts/stops it and reads immutable snapshots (lock-guarded), so
    // Unity's main thread never blocks on sockets. Receive uses a 500 ms
    // timeout so the loop wakes regularly and observes the stop flag within
    // one slice; StopSearch additionally closes the socket to unblock a
    // blocking Receive immediately. Stop is clean: the thread disposes the
    // socket itself, no leaked sockets or orphaned threads (mirrors
    // LanRoomBroadcaster). Malformed datagrams are silently ignored: strict
    // TryDecode rejects wrong magic/version/length/code/port/UTF-8, and
    // non-matching room codes never enter the results.
    public class LanRoomDiscovery : MonoBehaviour
    {
        // One discovered room: an immutable snapshot of the freshest matching
        // announcement received from its host.
        public sealed class DiscoveredRoom
        {
            public string RoomCode { get; }       // canonical code from the packet
            public IPAddress HostAddress { get; } // LAN address the announcement came from
            public ushort GamePort { get; }       // advertised NGO game port to connect to
            public string HostName { get; }       // optional advertised host name ("" when absent)
            public DateTime LastSeenUtc { get; }  // when the freshest announcement arrived

            internal DiscoveredRoom(string roomCode, IPAddress hostAddress, ushort gamePort,
                string hostName, DateTime lastSeenUtc)
            {
                RoomCode = roomCode;
                HostAddress = hostAddress;
                GamePort = gamePort;
                HostName = hostName;
                LastSeenUtc = lastSeenUtc;
            }

            // EndPoint the join step should connect to (host address + game port).
            public IPEndPoint ConnectEndPoint => new IPEndPoint(HostAddress, GamePort);
        }

        [Header("LAN Discovery (UDP listen)")]
        [Tooltip("UDP port PBJ hosts broadcast announcements on (must match LanRoomBroadcaster).")]
        [SerializeField] private int discoveryPort = LanDiscoveryPacket.DefaultDiscoveryPort;

        [Tooltip("Room code to search for (may also be passed to StartSearch instead).")]
        [SerializeField] private string roomCode = "";

        [Tooltip("Announcements older than this are stale (hosts re-announce every ~1s).")]
        [SerializeField] private float staleAfterSeconds = 5f;

        private const int ReceiveTimeoutMs = 500;    // loop wake-up slice; stop responds within this
        private const int ErrorLogThrottleMs = 2000; // keeps socket error spam readable
        private const float PruneAfterSeconds = 60f; // drop rooms no host refreshes anymore

        private Thread _worker;
        private volatile bool _stopRequested;
        private volatile UdpClient _client;  // created by the thread; closed by StopSearch to unblock Receive
        private volatile string _searchCode = "";
        private readonly object _storeLock = new object();
        private readonly Dictionary<string, DiscoveredRoom> _rooms = new Dictionary<string, DiscoveredRoom>(); // key: canonical room code
        private int _lastErrorLogTickCount = int.MinValue; // worker thread only

        // True while the listener thread is running and not stopping.
        public bool IsSearching => _worker != null && _worker.IsAlive && !_stopRequested;

        // Canonical room code currently searched for (empty when idle).
        public string SearchCode => _searchCode;

        // Convenience overload using the serialized room code.
        public bool StartSearch()
        {
            return StartSearch(roomCode);
        }

        // Starts (or re-targets while running) the LAN room search. Rooms
        // found for a DIFFERENT code are discarded. Returns false on invalid
        // configuration (nothing is started then). Repeated calls with the
        // same code are harmless.
        public bool StartSearch(string searchCode)
        {
            string code = RoomCode.Normalize(searchCode);
            if (!RoomCode.IsValid(code))
            {
                LogError($"Invalid room code '{searchCode}' - search not started");
                return false;
            }
            if (discoveryPort < 1 || discoveryPort > 65535)
            {
                LogError($"Invalid discovery port {discoveryPort} - search not started");
                return false;
            }

            lock (_storeLock)
            {
                if (!string.Equals(_searchCode, code, StringComparison.Ordinal))
                {
                    _rooms.Clear(); // results for another code must not leak out
                }
                // Swap the active code under the same lock that clears the
                // store, so the re-target is atomic relative to the worker
                // thread (RecordRoom re-checks under this same lock).
                _searchCode = code;
            }
            if (_worker == null || !_worker.IsAlive)
            {
                _stopRequested = false;
                _worker = new Thread(RunDiscoveryLoop)
                {
                    IsBackground = true, // never blocks app shutdown
                    Name = "PBJ.LanRoomDiscovery"
                };
                _worker.Start();
                Log($"Searching for room {code} on UDP {discoveryPort}");
            }
            return true;
        }

        // Stops the background thread and listener, waits briefly for a clean
        // exit. Safe to call multiple times; also invoked from
        // OnDisable/OnDestroy. Found rooms are kept (read GetDiscoveredRooms
        // after stopping, or ClearResults to drop them).
        public void StopSearch()
        {
            _stopRequested = true;
            var worker = _worker;
            _worker = null;
            _searchCode = "";
            _client?.Close(); // unblock Receive immediately; the thread disposes the client itself
            if (worker != null && worker.IsAlive && !worker.Join(2000))
            {
                LogError("LAN discovery thread did not stop within 2s (thread is background; it will die with the app)");
            }
        }

        // Fresh matching rooms (announcements within staleAfterSeconds),
        // sorted by room code. Returns a snapshot copy, so it is safe to
        // iterate on the main thread while new announcements keep arriving.
        public List<DiscoveredRoom> GetDiscoveredRooms()
        {
            var snapshot = new List<DiscoveredRoom>();
            DateTime now = DateTime.UtcNow;
            lock (_storeLock)
            {
                foreach (var room in _rooms.Values)
                {
                    if (IsFresh(room, now))
                    {
                        snapshot.Add(room);
                    }
                }
            }
            snapshot.Sort((a, b) => string.CompareOrdinal(a.RoomCode, b.RoomCode));
            return snapshot;
        }

        // Gets the freshest entry for one room code (case-insensitive).
        public bool TryGetRoom(string code, out DiscoveredRoom room)
        {
            room = null;
            string normalized = RoomCode.Normalize(code);
            if (!RoomCode.IsValid(normalized))
            {
                return false;
            }
            lock (_storeLock)
            {
                if (!_rooms.TryGetValue(normalized, out room) || !IsFresh(room, DateTime.UtcNow))
                {
                    room = null;
                    return false;
                }
            }
            return true;
        }

        // Drops all stored rooms (e.g. when leaving the room browser).
        public void ClearResults()
        {
            lock (_storeLock)
            {
                _rooms.Clear();
            }
        }

        private void OnDisable()
        {
            StopSearch();
        }

        private void OnDestroy()
        {
            StopSearch();
        }

        // ---------------- background thread ----------------

        private void RunDiscoveryLoop()
        {
            UdpClient client = null;
            try
            {
                client = CreateListener();
                _client = client; // let StopSearch unblock Receive below
                var remote = new IPEndPoint(IPAddress.Any, 0);
                while (!_stopRequested)
                {
                    byte[] data;
                    try
                    {
                        data = client.Receive(ref remote);
                    }
                    catch (SocketException ex)
                    {
                        if (_stopRequested)
                        {
                            break;
                        }
                        if (ex.SocketErrorCode == SocketError.TimedOut)
                        {
                            continue; // routine ReceiveTimeout wake-up: re-check flags, receive on
                        }
                        ThrottledLogError($"UDP receive failed ({ex.SocketErrorCode})"); // e.g. adapter change
                        continue;
                    }
                    catch (ObjectDisposedException)
                    {
                        break; // socket closed by StopSearch
                    }

                    // One datagram = one packet; strict decode rejects anything malformed.
                    if (!LanDiscoveryPacket.TryDecode(data, out LanDiscoveryPacket packet))
                    {
                        continue;
                    }

                    // Match by room code (case-insensitive); ignore other rooms.
                    if (!RoomCode.Matches(_searchCode, packet.RoomCode))
                    {
                        continue;
                    }

                    RecordRoom(packet, remote.Address);
                }
            }
            catch (Exception ex) // e.g. bind failure - never kill the thread silently
            {
                if (!_stopRequested)
                {
                    LogError($"LAN discovery listener failed: {ex.Message}");
                }
            }
            finally
            {
                // The thread owns disposal; Close() from StopSearch only
                // unblocked Receive. ReferenceEquals guards against touching
                // a client a restarted search may have installed meanwhile.
                if (ReferenceEquals(_client, client))
                {
                    _client = null;
                }
                client?.Dispose();
            }
        }

        // Listener for advertisement broadcasts: ReuseAddress so several PBJ
        // instances on one machine (e.g. MPPM multi-instance testing) can all
        // listen on the discovery port; ReceiveTimeout so the loop wakes
        // regularly even if StopSearch's Close() is never observed.
        private UdpClient CreateListener()
        {
            var client = new UdpClient(); // IPv4, not bound yet: configure before Bind
            bool configured = false;
            try
            {
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.ReceiveTimeout = ReceiveTimeoutMs;
                client.Client.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));
                configured = true;
                return client;
            }
            finally
            {
                if (!configured)
                {
                    client.Dispose(); // never leak the socket on a failed bind
                }
            }
        }

        private void RecordRoom(LanDiscoveryPacket packet, IPAddress hostAddress)
        {
            DateTime now = DateTime.UtcNow;
            lock (_storeLock)
            {
                // Re-check under the lock: StartSearch clears the store and
                // swaps the search code atomically, so an in-flight packet for
                // a PREVIOUS code must never be recorded after a re-target.
                if (!RoomCode.Matches(_searchCode, packet.RoomCode))
                {
                    return;
                }

                // Keyed by room code: a room's code is its unique identity on
                // the LAN; re-announcements refresh the entry, a host restart
                // moves it (newest announcement wins).
                _rooms[packet.RoomCode] = new DiscoveredRoom(packet.RoomCode, hostAddress,
                    packet.GamePort, packet.HostName, now);

                // Opportunistic cleanup so long sessions cannot grow the store.
                List<string> expired = null;
                foreach (var entry in _rooms)
                {
                    if ((now - entry.Value.LastSeenUtc).TotalSeconds > PruneAfterSeconds)
                    {
                        if (expired == null)
                        {
                            expired = new List<string>();
                        }
                        expired.Add(entry.Key);
                    }
                }
                if (expired != null)
                {
                    foreach (string key in expired)
                    {
                        _rooms.Remove(key);
                    }
                }
            }
        }

        private bool IsFresh(DiscoveredRoom room, DateTime now)
        {
            return (now - room.LastSeenUtc).TotalSeconds <= Mathf.Max(staleAfterSeconds, 1f);
        }

        private static void Log(string message)
        {
            Debug.Log($"[PBJ.LanRoomDiscovery] {message}");
        }

        private static void LogError(string message)
        {
            Debug.LogError($"[PBJ.LanRoomDiscovery] {message}");
        }

        private void ThrottledLogError(string message)
        {
            int now = Environment.TickCount;
            if (_lastErrorLogTickCount == int.MinValue || now - _lastErrorLogTickCount >= ErrorLogThrottleMs)
            {
                _lastErrorLogTickCount = now;
                LogError($"{message} (throttled)");
            }
        }
    }
}
