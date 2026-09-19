using PBJ.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Production UI controller for the PBJ multiplayer panel (LAN room flow).
// Owns ONLY UI orchestration: reads inputs, validates via RoomCode APIs,
// drives NetworkSession + LanRoomBroadcaster + LanRoomDiscovery in the
// correct order, and surfaces progress via StatusText / RoomCodeDisplay.
public class MultiplayerUIController : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField roomCodeInput;

    [Header("Buttons")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;

    [Header("Outputs")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI roomCodeDisplay;

    [Header("Networking (scene references)")]
    [SerializeField] private NetworkSession networkSession;
    [SerializeField] private LanRoomBroadcaster lanRoomBroadcaster;
    [SerializeField] private LanRoomDiscovery lanRoomDiscovery;

    [Tooltip("Seconds to wait for a matching LAN room before reporting Not found.")]
    [SerializeField] private float joinSearchTimeoutSeconds = 5f;

    private bool _isHosting;
    private bool _broadcastingOwned;
    private bool _isJoining;
    private bool _searchOwned;
    private string _joinRoomCode = string.Empty;
    private string _joinPlayerName = string.Empty;
    private float _searchDeadline;

    private void OnEnable()
    {
        SetButtonsInteractable(true);
    }

    private void Update()
    {
        if (!_isJoining || lanRoomDiscovery == null)
        {
            return;
        }
        if (lanRoomDiscovery.TryGetRoom(_joinRoomCode, out LanRoomDiscovery.DiscoveredRoom room) && room != null)
        {
            StopOwnedSearch();
            string hostAddress = room.HostAddress != null ? room.HostAddress.ToString() : string.Empty;
            SetStatus("Room found. Connecting...");
            bool connected = networkSession != null &&
                networkSession.StartClient(hostAddress, room.GamePort, _joinPlayerName);
            SetStatus(connected ? "Connected to room " + room.RoomCode + "." :
                "Connection failed. Check the host is still online and try again.");
            _isJoining = false;
            SetButtonsInteractable(true);
            return;
        }
        if (Time.time >= _searchDeadline)
        {
            StopOwnedSearch();
            _isJoining = false;
            SetStatus("Room " + _joinRoomCode + " not found. Check the code and try again.");
            SetButtonsInteractable(true);
        }
    }

    // Wired to CreateRoomButton.OnClick.
    public void CreateRoom()
    {
        if (_isHosting || _isJoining)
        {
            return;
        }
        string playerName = playerNameInput != null ? playerNameInput.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(playerName))
        {
            SetStatus("Enter a player name first.");
            return;
        }
        if (networkSession == null || lanRoomBroadcaster == null)
        {
            SetStatus("Host failed: networking references are missing.");
            return;
        }
        string roomCode = roomCodeInput != null ? RoomCode.Normalize(roomCodeInput.text) : string.Empty;
        if (string.IsNullOrEmpty(roomCode))
        {
            roomCode = RoomCode.Generate();
        }
        else if (!RoomCode.IsValid(roomCode))
        {
            SetStatus("Invalid room code. Use 4 characters (A-Z, 2-9).");
            return;
        }
        SetButtonsInteractable(false);
        if (!networkSession.StartHost(playerName))
        {
            SetStatus("Host failed to start. Try again.");
            SetButtonsInteractable(true);
            return;
        }
        if (!lanRoomBroadcaster.StartBroadcasting(roomCode, networkSession.GamePort, playerName))
        {
            SetStatus("Host started, but room advertising failed.");
            SetButtonsInteractable(true);
            return;
        }
        _isHosting = true;
        _broadcastingOwned = true;
        ShowRoomCode(lanRoomBroadcaster.CurrentRoomCode);
        SetStatus("Room " + lanRoomBroadcaster.CurrentRoomCode + " created. Waiting for players...");
    }

    // Wired to JoinRoomButton.OnClick.
    public void JoinRoom()
    {
        if (_isHosting || _isJoining)
        {
            return;
        }
        string playerName = playerNameInput != null ? playerNameInput.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(playerName))
        {
            SetStatus("Enter a player name first.");
            return;
        }
        string roomCode = roomCodeInput != null ? RoomCode.Normalize(roomCodeInput.text) : string.Empty;
        if (!RoomCode.IsValid(roomCode))
        {
            SetStatus("Enter a valid 4-character room code to join.");
            return;
        }
        if (networkSession == null || lanRoomDiscovery == null)
        {
            SetStatus("Join failed: networking references are missing.");
            return;
        }
        SetButtonsInteractable(false);
        if (!lanRoomDiscovery.StartSearch(roomCode))
        {
            SetStatus("Search failed to start. Try again.");
            SetButtonsInteractable(true);
            return;
        }
        _isJoining = true;
        _searchOwned = true;
        _joinRoomCode = roomCode;
        _joinPlayerName = playerName;
        _searchDeadline = Time.time + Mathf.Max(joinSearchTimeoutSeconds, 1f);
        SetStatus("Searching for room " + roomCode + "...");
    }

    private void OnDisable()
    {
        CleanupOwnedSystems();
    }

    private void OnDestroy()
    {
        CleanupOwnedSystems();
    }

    private void CleanupOwnedSystems()
    {
        StopOwnedSearch();
        if (_broadcastingOwned && lanRoomBroadcaster != null)
        {
            lanRoomBroadcaster.StopBroadcasting();
        }
        _broadcastingOwned = false;
        _isHosting = false;
        _isJoining = false;
    }

    private void StopOwnedSearch()
    {
        if (_searchOwned && lanRoomDiscovery != null)
        {
            lanRoomDiscovery.StopSearch();
        }
        _searchOwned = false;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void ShowRoomCode(string code)
    {
        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = string.IsNullOrEmpty(code) ? string.Empty : "Room: " + code;
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (createRoomButton != null)
        {
            createRoomButton.interactable = interactable;
        }
        if (joinRoomButton != null)
        {
            joinRoomButton.interactable = interactable;
        }
    }
}

