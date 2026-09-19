using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Visual-only world-space nameplate for a networked player.
// Attached to the EXISTING NameplateCanvas child of the player prefab.
// Reads the replicated display name from the NetworkPlayerIdentity on the
// SAME player NetworkObject and mirrors it into the existing NameText.
// No networking writes, no messages, no ownership changes.
public class PlayerNameplate : NetworkBehaviour
{
    [Header("Nameplate (visual only)")]
    [SerializeField] private TMP_Text nameText;

    private PBJ.Networking.NetworkPlayerIdentity _identity;
    private bool _subscribed;

    public override void OnNetworkSpawn()
    {
        // Same-player lookup only: this canvas is a child of the player root,
        // so walk up the hierarchy. Never searches the scene.
        _identity = GetComponentInParent<PBJ.Networking.NetworkPlayerIdentity>();
        if (_identity == null)
        {
            Debug.LogError("[PBJ][UI] PlayerNameplate requires a NetworkPlayerIdentity on the same player.", this);
            return;
        }

        ApplyName(_identity.DisplayNameText);
        _identity.DisplayName.OnValueChanged += OnDisplayNameChanged;
        _subscribed = true;
    }

    public override void OnNetworkDespawn()
    {
        if (_subscribed && _identity != null)
        {
            _identity.DisplayName.OnValueChanged -= OnDisplayNameChanged;
        }
        _subscribed = false;
        _identity = null;
    }

    private void OnDisplayNameChanged(FixedString64Bytes previous, FixedString64Bytes current)
    {
        ApplyName(current.ToString());
    }

    private void ApplyName(string displayName)
    {
        if (nameText == null)
        {
            return;
        }
        nameText.text = displayName;
    }
}
