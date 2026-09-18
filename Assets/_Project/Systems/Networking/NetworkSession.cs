using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace PBJ.Networking
{
    // M1-01 + M1-02: Network session foundation + Host/Client connection flow.
    // Reuses M1-01 setup (NetworkManager + UnityTransport, LAN/Direct IP, configurable port).
    // M1-02 adds observable connection lifecycle logging and safe connection-failure handling.
    // M3 LAN integration seams (LAN/direct-IP only; no UI here, no Relay/UGS/Lobby,
    // and deliberately NO automatic broadcaster start - minimum coupling):
    // - HOST UI: StartHost(playerName) -> on success the host UI itself starts
    //   LanRoomBroadcaster, advertising session.GamePort (the real NGO game
    //   port) + its RoomCode. The CLI host flow is unchanged.
    // - CLIENT UI: LanRoomDiscovery.StartSearch(roomCode) -> DiscoveredRoom ->
    //   StartClient(room.HostAddress.ToString(), room.GamePort, playerName),
    //   which reuses the ONE ApplyConnectionData()/SetConnectionData path.
    // - Both entry points queue the local player name into the NGO
    //   connection-approval payload via the existing PlayerSpawnHandler
    //   seam immediately before the NGO session starts; the server sanitizes it.
    public class NetworkSession : MonoBehaviour
    {
        [Header("Session Configuration (LAN / Direct IP)")]
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;

        // Distinguishes a normal client disconnect from an initial connection failure.
        private bool _hasConnected;

        // M3 LAN direct-connect fix: HOST/server mode binds UnityTransport's
        // listen endpoint to every local network interface. Without this,
        // UnityTransport uses the configured/CLI `address` as the listen
        // address too (PBJ default = 127.0.0.1), so the server would accept
        // loopback only and a real LAN client could never reach the
        // advertised LanRoomBroadcaster address:GamePort. 0.0.0.0 also keeps
        // loopback connections working, so same-machine testing is unchanged.
        private const string LanListenAddress = "0.0.0.0";

        // M3: read-only exposure of the ACTUAL NGO game port this session uses
        // (ApplyConnectionData pushes exactly this value into UnityTransport:
        // the host's listen port / the client's connect port). The future host
        // UI must pass THIS value to LanRoomBroadcaster.StartBroadcasting(...)
        // so LAN advertisements always match the real game port - never a
        // hardcoded one. Deliberately the only session value discovery needs
        // (clients learn the host address from the discovery datagram itself).
        public ushort GamePort => port;

        private void Awake()
        {
            ReadCommandLineArgs();
        }

        private void Start()
        {
            // Command-line driven startup: -pbjHost | -pbjClient [ip] | -pbjPort <port>
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-pbjHost") >= 0)
            {
                StartHost();
            }
            else if (Array.IndexOf(args, "-pbjClient") >= 0)
            {
                int i = Array.IndexOf(args, "-pbjClient");
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    address = args[i + 1];
                }
                StartClient();
            }
        }

        // Legacy entry point (CLI flags, scene wiring): unchanged
        // behavior, including not touching any already-queued name payload.
        public void StartHost()
        {
            StartHost(null);
        }

        // M3: name-aware host entry ("Create Room"). localPlayerName is queued
        // into the NGO connection-approval payload (the PlayerSpawnHandler
        // seam) at the only correct time: immediately BEFORE StartHost, while
        // NetworkConfig.ConnectionData is captured for this session. The host
        // is client 0 and flows through the same server-side sanitization as
        // every client (PlayerNameValidator fallback included). A null name
        // leaves any payload queued directly on PlayerSpawnHandler untouched.
        public bool StartHost(string localPlayerName)
        {
            SetConnectionPayloadIfProvided(localPlayerName);
            ApplyConnectionData(true); // host/server: bind all local interfaces (LAN)
            if (NetworkManager.Singleton.StartHost())
            {
                Log("HOST", "Starting HOST");
                Log("HOST", "Host started successfully");
                return true;
            }

            LogError("HOST", "Failed to start Host");
            return false;
        }

        // Legacy entry point (CLI flags, hotkeys, scene wiring): uses the
        // serialized/CLI-configured address:port; payload untouched.
        public void StartClient()
        {
            StartClientCore(null);
        }

        // M3: direct client entry for a discovered LAN room (or plain
        // direct IP). Accepts the endpoint surfaced by LanRoomDiscovery
        // (DiscoveredRoom.HostAddress + DiscoveredRoom.GamePort) and reuses
        // the ONE ApplyConnectionData()/UnityTransport.SetConnectionData path -
        // no second transport setup, no discovery-type dependency here.
        // Existing StartClient() stays valid. Returns false on invalid
        // endpoint data (session state untouched) or failed client start.
        public bool StartClient(string hostAddress, ushort gamePort, string localPlayerName = null)
        {
            if (string.IsNullOrWhiteSpace(hostAddress))
            {
                LogError("CLIENT", "StartClient rejected: host address is empty");
                return false;
            }
            if (gamePort == 0)
            {
                LogError("CLIENT", "StartClient rejected: game port 0 is invalid");
                return false;
            }

            address = hostAddress.Trim(); // same fields ApplyConnectionData consumes
            port = gamePort;
            return StartClientCore(localPlayerName);
        }

        private bool StartClientCore(string localPlayerName)
        {
            SetConnectionPayloadIfProvided(localPlayerName);
            ApplyConnectionData(false); // client: `address`/`port` = remote discovered endpoint
            if (NetworkManager.Singleton.StartClient())
            {
                _hasConnected = false;
                Log("CLIENT", "Starting CLIENT");
                Log("CLIENT", $"Connecting to {address}:{port}");
                return true;
            }

            LogError("CLIENT", $"Failed to start Client ({address}:{port})");
            return false;
        }

        // M3: queue the connection-approval payload only when a name was
        // provided through the M3 entry points; null means "legacy path -
        // leave whatever the caller queued on PlayerSpawnHandler alone".
        // Empty/whitespace names still flow to the server, whose
        // PlayerNameValidator falls back to Player#### deterministically.
        private static void SetConnectionPayloadIfProvided(string localPlayerName)
        {
            if (localPlayerName == null)
            {
                return;
            }
            PlayerSpawnHandler.SetConnectionPayload(localPlayerName);
        }

        private void ApplyConnectionData(bool isServer)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                LogError("NETWORK", "UnityTransport not found on NetworkManager");
                return;
            }

            // HOST: keep `address` as the configured/CLI (remote) address, but
            // listen on all local interfaces so LAN Direct IP clients can
            // connect. CLIENT: unchanged - `address`/`port` stay the remote
            // destination (the discovered HostAddress + GamePort); the client
            // has nothing to bind for LAN, so no listen address is passed.
            if (isServer)
            {
                transport.SetConnectionData(address, port, LanListenAddress);
            }
            else
            {
                transport.SetConnectionData(address, port);
            }

            transport.ConnectTimeoutMS = 2000;
            transport.MaxConnectAttempts = 1;
            transport.DisconnectTimeoutMS = 3000;
            transport.HeartbeatTimeoutMS = 1000;
        }

        private void OnEnable()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.LocalClientId)
            {
                Log("HOST", "Host session active (server listening)");
            }
            else if (NetworkManager.Singleton.IsServer)
            {
                Log("HOST", $"Client connected: {clientId}. Total clients: {NetworkManager.Singleton.ConnectedClients.Count}");
            }
            else if (NetworkManager.Singleton.IsClient && clientId == NetworkManager.Singleton.LocalClientId)
            {
                _hasConnected = true;
                Log("CLIENT", "Client connected");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                Log("HOST", $"Client disconnected: {clientId}. Total clients: {NetworkManager.Singleton.ConnectedClients.Count}");
            }
            else if (NetworkManager.Singleton.IsClient && clientId == NetworkManager.Singleton.LocalClientId)
            {
                if (_hasConnected)
                {
                    _hasConnected = false;
                    Log("CLIENT", "Client disconnected");
                }
                else
                {
                    Log("CLIENT", "Connection failed");
                }
            }
        }

        private void Log(string role, string message)
        {
            Debug.Log($"[PBJ][Network] [{role}] {message}");
        }

        private void LogError(string role, string message)
        {
            Debug.LogError($"[PBJ][Network] [{role}] {message}");
        }

        private void ReadCommandLineArgs()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-pbjPort" && i + 1 < args.Length)
                {
                    if (ushort.TryParse(args[i + 1], out ushort p))
                    {
                        port = p;
                    }
                }
            }
        }
    }
}

