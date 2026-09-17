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
   Test H (M2-02 NETWORKED pickup/drop): owner instance tekan E dekat `batu` (kubus di
   (1, 0.5, 1.5), komponen `PickupItem` + `M2NetworkedPickup` + NetworkObject +
   NetworkTransform). Host pickup -> client melihat batu nempel di player host; client
   pickup -> host melihat batu di player client. Q -> server unparent + drop di depan
   carrier (semua instance melihat hasilnya). Detail arsitektur & validasi server:
   lihat bagian "M2-02 networked pickup/drop" di bawah. Fallback lokal (item TANPA
   M2NetworkedPickup spawned) tetap jalan seperti dulu.

## M2-02 networked pickup/drop (server-authoritative)

```
E (owner instance, gate) -> PlayerInteraction.TryInteract -> PickupItem.Interact(player)
   -> M2NetworkedPickup.RequestPickup -> host: validasi server langsung;
      client: PickupServerRpc -> server validasi -> carrierClientId = sender
      -> NetworkObject.TrySetParent(player) + local (0,0,0.7) + collider off
Q (owner instance, gate) -> PlayerInventory.Drop -> M2NetworkedPickup.RequestDrop
   -> host: validasi langsung; client: DropServerRpc -> server validasi
      (hanya carrier) -> TrySetParent(null) + posisi = player.pos + forward*1.5
      + collider on -> NetworkTransform (server authority) mereplikasi hasil.
```

- **Authority**: SERVER. Item NetworkObject tetap server-owned (ownership TIDAK
  dipindah ke carrier). Carrier diwakili `carrierClientId`.
- **State**: satu `NetworkVariable<ulong> carrierClientId` (server write, everyone
  read) di `M2NetworkedPickup`. `Dropped = ulong.MaxValue` = item tersedia; nilai
  lain = sedang dipegang client tersebut. Tidak ada bool/enum tambahan, tidak ada
  inventory state di player, tidak ada manager/registry.
- **RPC**: 2 ServerRpc saja (`PickupServerRpc`, `DropServerRpc`,
  `RequireOwnership=false`; identity dari `ServerRpcParams.Receive.SenderClientId`,
  bukan id yang dikirim client). **Tidak ada ClientRpc** - visual mengikuti state
  ter-replikasi + NGO parenting.
- **Validasi pickup (server)**: sender punya player object valid; item `Dropped`;
  jarak item-player <= 2 (range sama dengan PlayerInteraction); player belum
  membawa item lain (scan server-side `FindObjectsByType<M2NetworkedPickup>`,
  bukan registry); item spawned.
- **Validasi drop (server)**: hanya `carrierClientId == senderClientId` yang boleh
  drop; posisi drop dihitung server (aturan sama dengan kode lokal:
  `player.position + player.forward * 1.5`).
- **Network parenting**: server `TrySetParent(playerNetworkObject)` saat pickup,
  `TrySetParent(null)` saat drop (NGO 2.x menyinkronkan parenting ke semua client).
  NetworkTransform item dimatikan selama di-parent (player punya NetworkTransform
  sendiri; NT item yang server-authoritative akan melawan parent transform) dan
  dinyalakan lagi saat drop. Tidak ada custom replication.
- **Tanpa prediksi lokal**: path networked TIDAK mem-parenting lokal sebelum server
  konfirmasi (tidak ada rollback/duplikat visual). Client non-host hanya request;
  hasilnya datang dari state server. `PlayerInventory.requestedNetworkedPickup`
  hanya pointer LOKAL (tidak direplikasi) supaya Q tahu item mana yang pernah
  diminta sebelum server mengonfirmasi.
- **Simultan**: dua pemain menekan E hampir bersamaan -> request pertama yang tiba
  di server menang; request kedua ditolak (carrier != Dropped). Tidak ada lock lain.
- **Compile/boundary**: semua kode networking di
  `Assets/Playground/Abim/M2_MultiplayerIntegration/`; satu-satunya modifikasi di
  luar folder itu adalah branch networked minimal di `Pickupitem.cs` dan
  `PlayerInventory.cs` (path lokal offline tetap utuh). Item TIDAK didaftarkan ke
  `M2TestNetworkPrefabs.asset` / `DefaultNetworkPrefabs.asset` (scene-placed).

## DISCONNECT PICKUP BEHAVIOR: TBD

Belum ada rule gameplay disconnect (auto-drop / destroy / respawn item saat carrier
disconnect). Secara teknis, player object yang despawn membawa item yang di-parent
akan ikut despawn (NGO default). Keputusan gameplay ditunda - jangan dianggap bug.

## Status

- Compile: (M2-02 pending verification via CLI batch compile.)
- Runtime MPPM: **UNTESTED** untuk M2-02 networked pickup/drop (butuh manual test).
- Physical LAN: NOT TESTED.
- Local/offline pickup/drop fallback: tetap tersedia untuk item non-networked.

## Known limitations

- M2-02 runtime belum diverifikasi manual (MPPM tidak bisa dijalankan dari CLI):
  matrix Host+1/2/3 Client, failure cases, dan perilaku late-join masih UNTESTED.
- Disconnect saat membawa item: item ikut despawn (NGO default) - gameplay rule TBD
  (lihat "DISCONNECT PICKUP BEHAVIOR: TBD").
- Item tidak punya Rigidbody/NetworkRigidbody (sengaja, M2-02): setelah drop item
  diam di posisi drop; fisika jatuh/rebound = task terpisah bila dibutuhkan.
- Interpolasi & jitter runtime belum diobservasi eksplisit (NetworkTransform
  Interpolate on; observasi runtime menunggu manual test).
- PlayerInteraction masih local-only: `Interact()` hanya jalan di instance yang
  menekan E (dan hanya owner via gate); tidak ada sync hasil interact.
- Movement masih XZ-plane placeholder 2.5D; final movement/gameplay dikembangkan
  di M2 oleh Ibnu (branch programmer/mechanics).
- Environment selain `batu` (ground, Wall) sengaja tidak dinetwork-kan: object
  statis, collider-nya sudah ada di setiap instance sehingga collision bekerja
  tanpa transform sync.
