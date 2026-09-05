using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace PBJ.Networking
{
    /// <summary>
    /// M1-01: Network session foundation bootstrap.
    /// Provides minimal Host / Client session start on top of NetworkManager + Unity Transport.
    /// LAN / Direct IP: address and port are configurable (Inspector fields or command line args).
    /// No gameplay logic. No player spawning (that is M1-02+).
    /// </summary>
    public class NetworkSession : MonoBehaviour
    {
        [Header("Session Configuration (LAN / Direct IP)")]
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;

        private void Awake()
        {
            ReadCommandLineArgs();
        }

        private void Start()
        {
            // Command-line driven startup (used by automated/batch testing):
            // -pbjHost            start as Host
            // -pbjClient [ip]     start as Client (optional ip override)
            // -pbjPort <port>     optional port override
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
                Debug.Log($"[NetworkSession] Host started on port {port}.");
            }
            else
            {
                Debug.LogError("[NetworkSession] Failed to start Host.");
            }
        }

        public void StartClient()
        {
            ApplyConnectionData();
            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log($"[NetworkSession] Client connecting to {address}:{port}...");
            }
            else
            {
                Debug.LogError($"[NetworkSession] Failed to start Client ({address}:{port}).");
            }
        }

        private void ApplyConnectionData()
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("[NetworkSession] UnityTransport not found on NetworkManager.");
                return;
            }
            transport.SetConnectionData(address, port);
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
            Debug.Log($"[NetworkSession] Client connected: {clientId}. Total clients (server view): {NetworkManager.Singleton.ConnectedClients.Count}");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[NetworkSession] Client disconnected: {clientId}.");
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
