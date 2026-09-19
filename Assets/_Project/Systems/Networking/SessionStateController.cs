using System;
using Unity.Netcode;
using UnityEngine;

namespace PBJ.Networking
{
    // A1: minimal server-authoritative session state for PBJ multiplayer.
    // - ONE NetworkVariable<SessionState> (default Waiting), everyone-read /
    //   server-write, replicated by Netcode for GameObjects to every client,
    //   including late joiners, who receive the current value at spawn.
    // - Only the server/host may transition Waiting -> Playing, and the
    //   ">= 2 connected players" requirement is enforced SERVER-SIDE here
    //   (UI-only enforcement is insufficient). Clients react to replicated
    //   changes through the SessionStateChanged event / OnValueChanged logs.
    // - Intentionally no gameplay logic: no Shaman/Offering/pickup/revive
    //   rules, no input gating (M2's M2NetworkInputGate stays untouched),
    //   no host migration/failover (A2), and no late-join spawn placement.
    // - The A1 start trigger is the host-only Start Game UI: the StartGameButton
    //   calls MultiplayerUIController.StartGame(), which calls this component's
    //   TryStartGame(); this server-side rule check stays the sole authority.
    public class SessionStateController : NetworkBehaviour
    {
        // A1 start rule: at least 2 connected players with a spawned player
        // object. Enforced in TryStartGame on the server (never UI-only).
        public const int MinPlayersToStart = 2;

        // Global seam for A1+ consumers (UI, gameplay): fires on every
        // instance when the replicated state changes. A1 itself only logs;
        // nobody subscribes yet, so the disabled-domain-reload leak risk of
        // static events is not introduced by this task.
        public static event Action<SessionState> SessionStateChanged;

        // Server-write / everyone-read, same authority pattern as the
        // NetworkPlayerIdentity display name. Default state: Waiting.
        private readonly NetworkVariable<SessionState> _state = new NetworkVariable<SessionState>(
            SessionState.Waiting,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // Read-only view of the replicated session state (Waiting/Playing).
        public SessionState State => _state.Value;

        public bool IsPlaying => _state.Value == SessionState.Playing;

        // Server/host-only start request. Returns false (changing nothing)
        // when called on a client, when the session is already Playing, or
        // when the server-side requirement is not met: at least
        // MinPlayersToStart players must have a spawned player object.
        // UI may pre-check for feedback, but THIS check is authoritative.
        public bool TryStartGame()
        {
            if (!IsServer)
            {
                Debug.LogWarning("[PBJ][Network][SESSION] TryStartGame rejected: only the host/server may start the game");
                return false;
            }
            if (_state.Value != SessionState.Waiting)
            {
                Debug.LogWarning($"[PBJ][Network][SESSION] TryStartGame rejected: session already {_state.Value}");
                return false;
            }

            int playersWithPlayerObject = CountPlayersWithPlayerObject();
            if (playersWithPlayerObject < MinPlayersToStart)
            {
                Debug.LogWarning($"[PBJ][Network][SESSION] TryStartGame rejected: {playersWithPlayerObject}/{MinPlayersToStart} players with spawned player objects (start requires >= {MinPlayersToStart})");
                return false;
            }

            // Server write -> NGO replicates to every client (and to late
            // joiners through the spawn-time snapshot).
            _state.Value = SessionState.Playing;
            Debug.Log($"[PBJ][Network][SESSION] Session state -> Playing ({playersWithPlayerObject} players)");
            return true;
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"[PBJ][Network][SESSION] Session state on spawn: {_state.Value} ({(IsServer ? "server/host" : "client")})");

            // Unsubscribe first for idempotency (scene objects can go through
            // multiple sessions with disabled domain reload), then subscribe.
            _state.OnValueChanged -= OnStateChanged;
            _state.OnValueChanged += OnStateChanged;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            _state.OnValueChanged -= OnStateChanged;
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        // A1 client reaction to replicated state changes: log + event seam.
        // Note: OnValueChanged does not fire for the value a late joiner
        // receives at spawn time - that snapshot is surfaced by OnNetworkSpawn.
        private void OnStateChanged(SessionState previousValue, SessionState newValue)
        {
            Debug.Log($"[PBJ][Network][SESSION] Session state changed: {previousValue} -> {newValue}");
            SessionStateChanged?.Invoke(newValue);
        }

        private void OnClientConnected(ulong clientId)
        {
            if (IsServer)
            {
                LogServerPlayerCount("connected");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (IsServer)
            {
                LogServerPlayerCount("disconnected");
            }
        }

        // Server observability for the >= 2 start rule (A1 minimum; no
        // connection logic is changed by this component).
        private void LogServerPlayerCount(string reason)
        {
            Debug.Log($"[PBJ][Network][SESSION] Player {reason}; players with player objects: {CountPlayersWithPlayerObject()} (start requires >= {MinPlayersToStart})");
        }

        private int CountPlayersWithPlayerObject()
        {
            int count = 0;
            foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
            {
                if (client.PlayerObject != null)
                {
                    count++;
                }
            }
            return count;
        }
    }
}