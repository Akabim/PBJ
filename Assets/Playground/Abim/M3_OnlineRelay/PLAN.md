# M3 — Online Relay Foundation (Implementation Plan)

> Status: PLAN (audit complete, no code written yet).
> Prepared after M2-02 checkpoint commit `ef8f125`.
> Scope: online 1 Host + up to 3 Clients over Unity Relay, join by code,
> player-entered display name. NO matchmaking, NO public server browser,
> NO dedicated servers, NO host migration, NO voice chat, NO menu redesign.

## 0. Hard constraints (from M2 checkpoint)

- M2-02 runtime-verified behavior must not regress: scene-placed `batu`
  (GlobalObjectIdHash 1429912368) pickup/drop sync, server-authoritative.
- Direct IP / LAN flow (`NetworkSession.cs`) stays fully functional during M3.
- No second `NetworkManager`, no rewrite of M2 networking. Gameplay remains NGO
  server-authoritative; Relay is only the transport path.

## 1. Audit result (current state, 2026-09-18)

| Item | State |
|---|---|
| `com.unity.services.multiplayer` | NOT installed |
| `com.unity.services.authentication` | NOT installed |
| UGS initialized anywhere in project code | NO (no references to `UnityServices`, `AuthenticationService`, `MultiplayerService`) |
| Anonymous auth implemented | NO |
| Project linked to Unity Dashboard / UGS | NO (`cloudProjectId` empty, `UnityConnectSettings.m_Enabled: 0`, no `Assets/Services` folder) |
| Existing UI (Canvas/TMP/EventSystem) in any scene | NONE |
| TextMeshPro | Available (bundled in `com.unity.ugui 2.0.0`, Unity 6) |
| MPPM 2.0.2 | Installed (local multi-instance testing possible) |
| `com.unity.multiplayer.center 1.0.1` | Present — Multiplayer Center window only, NOT the sessions SDK |
| NGO | `com.unity.netcode.gameobjects 2.7.0` |
| Transport | `com.unity.transport 2.7.4` (transitive dep of NGO) |
| Editor | `6000.3.23f1` |

### Existing architecture to reuse (do NOT duplicate)

- `Assets/_Project/Systems/Networking/NetworkSession.cs` (`PBJ.Networking`):
  public `StartHost()` / `StartClient()`, `ApplyConnectionData()`
  (`transport.SetConnectionData(address, port)` + aggressive LAN timeouts),
  command-line flags (`-pbjHost`, `-pbjClient [ip]`, `-pbjPort`), H/C hotkeys,
  connect/disconnect logging via NGO callbacks, `_hasConnected` distinction.
- `Assets/_Project/Systems/Players/PlayerSpawnHandler.cs`:
  server-authoritative spawning via `NetworkManager.ConnectionApprovalCallback`
  (approve + `CreatePlayerObject` + deterministic position spacing).
- `Assets/_Project/Systems/Players/NetworkPlayerIdentity.cs` (`PBJ.Networking`,
  on `Player.prefab`): `OnNetworkSpawn` renames object to `Player_{OwnerClientId}`,
  ownership verification logging. This is the single player-identity component —
  display-name support goes HERE (no second identity system).
- Target scene for the online prototype: `PBJ_MultiplayerPrototype.unity`
  (already in build settings; contains NetworkManager + UnityTransport +
  NetworkSession + PlayerSpawnHandler + M2-02 batu).

## 2. Target stack (verified API names — Multiplayer Services SDK 2.1.x)

```
NetworkSession / OnlineSession (PBJ)
  -> Unity Multiplayer Services (MultiplayerService.Instance, Unity.Services.Multiplayer)
    -> Relay (WithRelayNetwork — allocation handled by the SDK, NOT RelayService directly)
      -> UnityTransport (SetRelayServerData handled internally by the SDK)
        -> Netcode for GameObjects (2.7.0, unchanged)
          -> existing PBJ network gameplay (M2-02, unchanged)
```

Verified API surface (docs: Multiplayer Services SDK 2.1.1):

- Host: `new SessionOptions { Name = ..., MaxPlayers = 4, PlayerProperties = ... }.WithRelayNetwork()`
  then `await MultiplayerService.Instance.CreateSessionAsync(options)` →
  `session.Code` (human-friendly join code; session ID ≠ join code).
- Client: `await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode)`.
- Player properties: `PlayerProperty` (string value + `VisibilityPropertyOptions`
  visibility; default Public). Set on both host and client via
  `SessionOptions.PlayerProperties["display_name"]` / `JoinSessionOptions`
  (both inherit `BaseSessionOptions.PlayerProperties`).
- Optional: `SessionOptionsExtensions.WithPlayerName()` exists, but we manage
  our own `display_name` property (do not depend on Auth's generated player name).
- Disconnect: `session.LeaveAsync()` (client) / host leaves by closing session.
- DO NOT use the deprecated standalone flow
  (`RelayService.Instance.CreateAllocationAsync` + `GetJoinCodeAsync` +
  manual `SetRelayServerData`). The SDK does allocation + transport wiring.

## 3. Required packages & configuration (step 0)

1. Package Manager → Unity Registry → install:
   - `Multiplayer Services` (`com.unity.services.multiplayer`, 2.1.1)
   - `Authentication` (`com.unity.services.authentication`, latest compatible)
   - Transitive deps auto-resolve: `com.unity.services.core`, Relay, QoS,
     `com.unity.nuget.newtonsoft-json`.
2. Editor: Project Settings → Services → link project to a Unity Dashboard
   project (requires org + project creation) → this fills `cloudProjectId`.
3. Unity Dashboard: enable **Authentication** (anonymous sign-in), **Relay**,
   and the **Multiplayer/Sessions** service for that project.
4. MPPM testing note: each PlayMode instance must use a DISTINCT auth profile
   (`AuthenticationService.Instance.SwitchProfile($"pbj-{instanceId}")`) before
   anonymous sign-in, otherwise multiple local instances fight over one profile.

## 4. Exact files for M3

### New: `Assets/_Project/Systems/Networking/OnlineSession.cs`
`PBJ.Networking` — MonoBehaviour placed next to NetworkSession in
`PBJ_MultiplayerPrototype` (serialized reference to the existing
`NetworkSession`).

Responsibilities:
- `EnsureUgsAsync()`: `UnityServices.InitializeAsync()` (guard: once) +
  `AuthenticationService.Instance.SignInAnonymouslyAsync()` (guard: `IsSignedIn`)
  + MPPM profile switch.
- `CreateRoomAsync(displayName)`:
  1. sanitize/validate name (see PlayerNameValidator),
  2. `SessionOptions { Name = "pbj", MaxPlayers = 4,
     PlayerProperties = { ["display_name"] = new PlayerProperty(name) } }`
     `.WithRelayNetwork()`,
  3. `await MultiplayerService.Instance.CreateSessionAsync(options)`,
  4. surface `session.Code` to UI,
  5. verify NGO host side: `NetworkManager.Singleton.IsHost` after session
     create. If NOT active, call `networkSession.StartHost()` as fallback
     (flag below prevents direct-data stomp).
- `JoinRoomAsync(displayName, joinCode)`:
  1. sanitize name; normalize code (trim, upper, strip inner spaces),
  2. `JoinSessionOptions` with the same `display_name` property,
  3. `await MultiplayerService.Instance.JoinSessionByCodeAsync(code, options)`,
  4. verify `NetworkManager.Singleton.IsClient && IsConnectedClient`; fallback:
     `networkSession.StartClient()`.
- Error surfacing: catch `SessionException` (+ `WebRequestException`) →
  status events for UI (`OnSessionStatus`, `OnJoinCodeReady`, `OnSessionError`).
- Teardown: hook NGO `OnClientDisconnectCallback` (NetworkSession already logs)
  + `session.LeaveAsync()` on teardown / scene exit.
- Relax transport timeouts when relayed (LAN values 2000/3000/1000 ms are too
  aggressive for internet paths): after session create/join set
  `transport.HeartbeatTimeoutMS = 5000`, `DisconnectTimeoutMS = 15000`.

### Minimal additive change: `NetworkSession.cs` (NOT a rewrite)
- Add `public bool SkipDirectConnectionData { get; set; }` (default false).
  In `ApplyConnectionData()`: skip `SetConnectionData(address, port)` when true
  (relay data set by the SDK must not be stomped). Timeouts may still be
  applied/overridden by OnlineSession.
- Everything else (logging, cmd-line flow, H/C hotkeys, Direct IP) untouched.

### Reuse + extend: `Assets/_Project/Systems/Players/NetworkPlayerIdentity.cs`
- Add `NetworkVariable<FixedString64Bytes> DisplayName`
  (server write, everyone read) + public read accessor.
- Value source of truth: session player property `display_name`.
- Host writes it at approval/spawn: `PlayerSpawnHandler` (which already owns the
  `ConnectionApprovalCallback`) reads the joining client's session player
  property via the active `ISession`, sanitizes server-side, then writes the
  NetworkVariable. The server never trusts raw client strings: validation
  happens at UI entry, at session property write, and again at server read.
- `OnNetworkSpawn` logs include the display name (visual name UI is M3+ scope).

### New: `Assets/_Project/Systems/Players/PlayerNameValidator.cs`
Static, `PBJ.Networking`: trim, collapse whitespace, strip control characters,
hard cap length (16 chars, FixedString64Bytes-safe), min length 1 → else
fallback `"Player####"` (derived from clientId, cosmetic only). One shared
implementation used by UI, OnlineSession, and the server-side read.

### New: minimal UI (no menu redesign)
- `Assets/_Project/Systems/UI/OnlineSessionUI.cs`: drives OnlineSession.
- Scene addition to `PBJ_MultiplayerPrototype.unity` (same scene — M2 batu
  hashes untouched): Canvas `OnlineSessionCanvas`:
  - `PlayerName` TMP_InputField (max length 16),
  - `JoinCode` TMP_InputField (client only),
  - `CREATE ROOM` Button → `OnlineSession.CreateRoomAsync(name)`,
  - `JOIN` Button → `OnlineSession.JoinRoomAsync(name, code)`,
  - `JoinCodeDisplay` TMP_Text (shows `session.Code`, read-only),
  - `Status` TMP_Text (auth/session/network state + errors).
- No scene-UI exists today; this is additive. Direct-IP hotkeys remain.

## 5. Flow (target)

HOST: name `[Abim]` → [CREATE ROOM] → EnsureUgsAsync → anonymous auth →
`CreateSessionAsync(SessionOptions{MaxPlayers=4}.WithRelayNetwork(),
PlayerProperties["display_name"]=name)` → display `session.Code` →
NGO host active (server-authoritative gameplay identical to M2).

CLIENT: name `[Nesya]`, code `[ABC123]` → [JOIN] → EnsureUgsAsync →
`JoinSessionByCodeAsync(code, options)` → SDK connects client through the
session's Relay network → NGO client connects to host → same game session →
`PlayerSpawnHandler` approves → `NetworkPlayerIdentity` carries the sanitized
display name for future name UI.

## 6. Verification checklist (M3 acceptance)

1. Clean tree; compile via CLI batch (repo pattern, same as M2-02 compile check).
2. Direct IP regression: `-pbjHost` / `-pbjClient` flow unchanged; M2 batu
   pickup/drop still passes (Host pickup → Client sees carrying; drop sync).
3. Editor build: create room with MaxPlayers=4 → join code displayed.
4. Second instance (MPPM, distinct auth profile) joins by code → same scene →
   both see each other's player objects; batu pickup/drop sync works over Relay.
5. Name: each instance sees the other's sanitized display name (log level OK
   for M3; visual name UI is future scope).
6. Full 1 Host + 3 Clients session; join failure (bad code), full-session join
   rejection (5th player), and host-quit teardown all log cleanly and return
   to menu state without orphaned NGO objects.

## 7. Risks / open points

- NGO auto-start: evidence (docs "netcode integration out of the box",
  delayed-start example wording, host auto-'Host' role assignment) says the SDK
  starts the NGO connection when a network is configured at create/join. The
  implementation verifies this at runtime (`IsHost`/`IsConnectedClient` checks)
  and falls back to `NetworkSession.StartHost()/StartClient()` — both paths
  designed, no guessing needed at code time.
- Join code visibility: `session.Code` is the ONLY code handle (session ID is
  not a code). Display it prominently; M3 has no server browser (by design).
- Relay region: default region is fine for the prototype; `RelayNetworkOptions`
  region selection is a later optimization, out of M3 scope.
- First-run-only: package install + dashboard linking + service enablement
  (Authentication/Relay/Sessions) must be done once in the Editor by a human
  with dashboard access before any code will run online.



