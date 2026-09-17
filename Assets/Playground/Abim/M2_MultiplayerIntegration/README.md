# M2 — Multiplayer Integration Test Environment

**Branch:** `programmer/systems` · **Sumber gameplay:** `programmer/mechanics` @ `00a2ef8` (Ibnu, cherry-pick `418a73c`)

## Apa ini

Integration test environment: duplikat scene prototype gameplay Ibnu
(`Assets/Playground/Ibnu/1.unity`) dengan player yang di-network-kan memakai
sistem networking PBJ yang sudah ada (M1-03 spawn, M1-04 ownership, M1-06
NetworkTransform).

> ⚠️ **BUKAN** production scene. **BUKAN** pengganti scene Ibnu. Scene Ibnu
> `1.unity` tidak diubah.

## Isi folder

| File | Fungsi |
|---|---|
| `PBJ_MultiplayerPrototype.unity` | Scene test (duplikat scene Ibnu + NetworkManager/session PBJ; environment tetap local/static) |
| `PBJ_M2TestPlayer.prefab` | Player networked: gameplay components Ibnu (unguarded) + `PlayerInventory` + child `HoldPoint` (**TEST-ONLY** config) + `M2NetworkInputGate` (NGO) + NetworkObject + NetworkTransform + NetworkRigidbody |
| `M2TestNetworkPrefabs.asset` | NetworkPrefabsList scene-local (berisi PBJ_M2TestPlayer) |
| `M2NetworkInputGate.cs` | Ownership gate: hanya owner instance yang memproses input (enable/disable components Ibnu, termasuk `PlayerInventory`, berdasar `IsOwner` + OnNetworkSpawn) |
| `M2LocalCameraBinder.cs` | Local-only camera binder: kamera instance masing-masing mengikuti player milik local client (`IsOwner`), tanpa sync apa pun |

## Architecture (yang dibuktikan di task ini)

```
PlayerMovement (Ibnu, unmodified) ─→ Rigidbody ─→ NetworkTransform ─→ remote client
PlayerFacing  (Ibnu, unmodified) ─→ Transform.rotation ─→ NetworkTransform ─→ remote client
CameraFollow  (Ibnu, unmodified) ─→ LOCAL ONLY (binder per-instance, no sync)
PlayerInteraction (Ibnu, unmodified) ─→ LOCAL PROTOTYPE ONLY (belum networked — out of scope)
PlayerRespawn (Ibnu, unmodified) ─→ owner-only via gate; posisi ditransfer NetworkTransform
PlayerInventory (Ibnu, unmodified) ─→ owner-only via gate; Q=drop, pickup via E→PickupItem — SEMUA LOCAL, belum networked
NetworkRigidbody (bawaan NGO) ─→ non-owner replica otomatis kinematic; velocity di-sync
Environment (batu/ground/Wall) ─→ STATIC/local, tidak dinetwork-kan (collider sudah ada di tiap instance)
```

- **Tanpa** ServerRpc/ClientRpc movement, **tanpa** NetworkVariable<Vector3>,
  **tanpa** custom transform replication, **tanpa** custom authority framework.
- NetworkTransform: **Owner authority** (config proven M1-06) + Interpolate on;
  sync **position + rotation** (rotation dibutuhkan PlayerFacing). Scale off
  (tidak relevan untuk player capsule).
- Ownership gating: **1 komponen gate tipis** (`M2NetworkInputGate`, NGO
  NetworkBehaviour) men-enable/disable components Ibnu berdasar `IsOwner` —
  script Ibnu tidak diubah sama sekali (plain MonoBehaviour yang membaca
  `Keyboard.current` selalu memproses input lokal tanpa guard; replica yang
  "bergerak" tanpa guard akan dikoreksi NetworkTransform tiap tick → jitter).
  Perubahan minimum ini reversible: hapus gate → kembali ke behavior original.
- Spawning memakai M1-03 yang sudah ada (`NetworkSession` → approval →
  `NetworkPlayerIdentity`); NetworkManager pada scene test adalah **satu
  NetworkManager** (bukan duplicate) yang di-setup dengan NetworkConfig dan
  NetworkPrefabsList scene-local (`M2TestNetworkPrefabs.asset` → PBJ_M2TestPlayer).
- `PlayerInventory.holdPoint` pada prefab test diisi child kosong **HoldPoint** (config TEST-ONLY,
  meniru scene Ibnu: empty child, localPos (0,0,0.7)). Bukan struktur final — final ditentukan
  di production scene (ownership scene: Abim).

## Cara test (MPPM: Host + 1 Client)

1. Buka `PBJ_MultiplayerPrototype.unity` → jendela Multiplayer Play Mode,
   aktifkan 1 Player Instance → Play (window MPPM).
2. **Host**: tekan **H** (Start Host). **Client**: tekan **C** (Start Client).
3. Test A: host WASD → client melihat Player_0 bergerak. Test B: client WASD →
   host melihat Player_1 bergerak. Test C: input instance lain TIDAK mengontrol
   player milik instance lain (ownership). Test D: facing/rotation tersinkron
   (rotation sync NetworkTransform aktif). Test E: kamera tiap instance mengikuti
   player miliknya (local only). Test F: collision — Rigidbody + CapsuleCollider;
   player tidak menembus ground; player-player collision bila ditempatkan
   berdekatan. Test G: spawning M1-03 tetap bekerja; tidak ada duplicate player;
   disconnect/despawn bersih.
   Test H (LOCAL, belum networked): owner instance tekan E dekat PickupItem → item nempel di
   HoldPoint; tekan Q → item drop. TIDAK ada sync — remote instance tidak melihat item (sengaja,
   out of scope). Objek pickup TEST-ONLY ada di scene ini: `batu` (kubus di (1, 0.5, 1.5)) dengan
   komponen `PickupItem` — dekat spawn P0/P1, TANPA networking sama sekali (local object saja).

## Status

- Compile: PASS (verified via CLI).
- Runtime MPPM: **UNTESTED** (menunggu manual test; CLI smoke test host PASS).
- Physical LAN: NOT TESTED.
- PlayerInteraction / pickup / drop: **BELUM NETWORKED** (sengaja — out of scope).

## Known limitations

- Interpolasi & jitter runtime belum diobservasi eksplisit (NetworkTransform
  Interpolate on; observasi runtime menunggu manual test).
- `PlayerInventory`/`PickupItem` juga masih local-only (tanpa authority network) —
  sengaja TIDAK di-sync di task ini; sync pickup/drop = task terpisah.
  Targeting sudah diperbaiki: `PlayerInteraction` meneruskan player pemicu ke
  `Interact(player)` sehingga item masuk ke inventory milik player itu
  (bukan lagi `FindFirstObjectByType<PlayerInventory>()` global).
- PlayerInteraction masih local-only: `Interact()` hanya jalan di instance yang
  menekan E (dan hanya owner via gate); tidak ada sync hasil interact.
- Movement masih XZ-plane placeholder 2.5D; final movement/gameplay dikembangkan
  di M2 oleh Ibnu (branch programmer/mechanics).
- Environment (batu, ground, Wall) sengaja tidak dinetwork-kan: object statis,
  collider-nya sudah ada di setiap instance sehingga collision bekerja tanpa
  transform sync. Kalau kelak environment menjadi dynamic, barulah perlu
  NetworkObject (bukan bagian dari task ini).
