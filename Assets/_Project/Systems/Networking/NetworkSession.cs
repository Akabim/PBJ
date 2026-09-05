using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace PBJ.Networking
{
    // M1-01 + M1-02: Network session foundation + Host/Client connection flow.
    // Reuses M1-01 setup (NetworkManager + UnityTransport, LAN/Direct IP, configurable port).
    // M1-02 adds observable connection lifecycle logging and safe connection-failure handling.
    public class NetworkSession : MonoBehaviour
    {
        [Header("Session Configuration (LAN / Direct IP)")]
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;

        // Distinguishes a normal client disconnect from an initial connection failure.
        private bool _hasConnected;

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

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
            {
                StartHost();
            }
            else if (keyboard != null && keyboard.cKey.wasPressedThisFrame)
            {
                StartClient();
            }
#endif
        }

        public void StartHost()
        {
            ApplyConnectionData();
            if (NetworkManager.Singleton.StartHost())
            {
                Log("HOST", "Starting HOST");
                Log("HOST", "Host started successfully");
            }
            else
            {
                LogError("HOST", "Failed to start Host");
            }
        }

        public void StartClient()
        {
            ApplyConnectionData();
            if (NetworkManager.Singleton.StartClient())
            {
                _hasConnected = false;
                Log("CLIENT", "Starting CLIENT");
                Log("CLIENT", $"Connecting to {address}:{port}");
            }
            else
            {
                LogError("CLIENT", $"Failed to start Client ({address}:{port})");
            }
        }

        private void ApplyConnectionData()
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                LogError("NETWORK", "UnityTransport not found on NetworkManager");
                return;
            }
            transport.SetConnectionData(address, port);
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

