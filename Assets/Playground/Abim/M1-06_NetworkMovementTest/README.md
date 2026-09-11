# M1-06 NetworkMovementTest

> **STATUS: TEMPORARY TEST PROTOTYPE.**
> Ini **BUKAN** movement gameplay final dan **BUKAN** replacement permanen untuk M1-05.
> `TemporaryPlayerMovement` hanyalah **temporary test driver M1-06** untuk membuktikan
> NetworkTransform — nantinya digantikan movement/character controller final
> (M1-05 / prototype gameplay M2) saat integrasi gameplay dimulai.
> M1-06 **tidak** menentukan final gameplay movement.

## Tujuan Prototype

Membuktikan (verify) **M1-06: sinkronisasi movement via NetworkTransform**:

- Perubahan posisi pada player lokal (owner) **ter-replikasi** ke host/instance
  lain **melalui NetworkTransform** — bukan lewat RPC atau kode custom.
- Hanya instance yang memiliki player (owner/authority) yang merespons input;
  instance remote **tidak** merespons input lokal.

## Cara Kerja

- `TemporaryPlayerMovement.cs` hanyalah **local input driver** sederhana:
  WASD 8-arah (diagonal dinormalisasi) di bidang XZ, `transform.position += move * speed * dt`.
  Tidak ada gravity, collision logic, acceleration, sprint, animation, atau movement feel.
- Sinkronisasi posisi 100% menjadi tanggung jawab komponen **NetworkTransform bawaan NGO**.
  Script ini **TIDAK menambahkan networking authority baru** (tidak ada ServerRpc/ClientRpc/NetworkVariable).
- Script membaca `NetworkTransform.AuthorityMode` untuk menentukan siapa yang
  boleh menggerakkan (server-authority → host; owner-authority → owning client),
  sehingga tetap benar di konfigurasi NetworkTransform mana pun.
- Prefab `Player_M106Test.prefab` adalah **prefab variant** dari production
  `Assets/_Project/Systems/Players/Player.prefab` yang hanya:
  1. menambahkan komponen **NetworkTransform** (Owner authority — lihat
     [Keputusan Authority](#keputusan-authority-test-prefab-saja), Interpolate on), dan
  2. menambahkan `TemporaryPlayerMovement`.

## Pemisahan Tanggung Jawab

**A. Networking proof (tanggung jawab M1-06)**
- NetworkTransform bawaan NGO (tanpa custom replication), Owner authority,
  sinkronisasi Host↔Client dua arah, ownership isolation, reconnect/despawn.
- Layer ini dibuat **generic**: driver movement apa pun yang mengubah
  Transform/Rigidbody pada instance otoritatif akan ter-replikasi tanpa
  membutuhkan arsitektur networking baru.

**B. Sumber movement**
- `TemporaryPlayerMovement.cs` = **temporary test driver M1-06** — bukan movement
  gameplay final, bukan replacement permanen M1-05; dipakai hanya untuk menguji
  NetworkTransform dan mereproduksi test M1-06.
- `PlayerMovement` (Ibnu, M1-05, branch `programmer/mechanics`) = gameplay movement.
- M1-05 / M2 movement dapat terus berkembang; M1-06 tidak menentukan final
  gameplay movement.

**C. Arah integrasi**
- M1-06 tidak menambahkan ServerRpc/ClientRpc/NetworkVariable/custom transform
  replication/abstraction layer — movement gameplay berikutnya cukup mengubah
  Transform/Rigidbody di instance owner dan membiarkan NetworkTransform bekerja.
- Hasil compatibility terhadap `PlayerMovement` Ibnu: **COMPATIBLE** — lihat
  [Kompatibilitas dengan PlayerMovement (M1-05)](#kompatibilitas-dengan-playermovement-m1-05).

## Keputusan Authority (TEST PREFAB SAJA)

- `NetworkTransform` pada variant ini di-set **Owner** (`AuthorityMode: Owner`).
  Alasan: driver movement berjalan di instance **pemilik** player (client menggerakkan
  player-nya sendiri, host menggerakkan `Player_0`), dan itu yang mau diverifikasi
  di M1-06 — pergerakan client harus ter-replikasi ke host dan sebaliknya.
- Ini **bukan** perubahan keputusan authority M1-04 global: production
  `Player.prefab`, `PlayerSpawnHandler`, `NetworkPlayerIdentity`, dan sistem lain
  **tidak disentuh**. Jika tim nanti memilih Server authority, cukup ubah satu field
  di Inspector — script movement sudah mengakomodasi keduanya (server-authority →
  hanya host yang bisa menggerakkan; owner-authority → hanya owner yang bisa).

## Input yang Digunakan

- **WASD** saja (Unity Input System package — sesuai setting project).
- W = maju (Z+), S = mundur (Z−), A = kiri (X−), D = kanan (X+), kombinasi diagonal didukung.

## Cara Test (MPPM)

1. Daftarkan prefab variant `Player_M106Test` di **DefaultNetworkPrefabs** dan set
   sebagai **PlayerPrefab** di NetworkManager (`SampleScene`). Ini perubahan
   **temporary untuk test saja** — jangan ikut di-commit; kembalikan kedua file
   (`Assets/Scenes/SampleScene.unity`, `Assets/DefaultNetworkPrefabs.asset`)
   setelah test selesai.
2. Buka **Multiplayer Play Mode** window, aktifkan minimal 1 Player Instance
   (virtual player), pastikan keduanya Play Mode Only.
3. Instance utama (main editor): tekan **H** (Start Host). Instance kedua: tekan
   **C** (Start Client). Tunggu log `[PBJ][Network] [CLIENT] Client connected`.
4. Gerakkan player lokal dengan WASD; amati instance lain.
5. Regression spawn/despawn: hentikan Play pada instance client, jalankan lagi,
   tekan **C** — client baru harus dapat **ClientId ≥ 2** (ID tidak dipakai ulang)
   dan player lama client pertama harus despawn saat disconnect.

## Kompatibilitas dengan PlayerMovement (M1-05)

Compatibility check (design/technical review — M1-05 **tidak** diintegrasikan sekarang;
branch `programmer/mechanics` tidak di-checkout/merge/ubah; referensi read-only:
commit `7138c9fe`, `Assets/Playground/Ibnu/Scripts/PlayerMovement.cs`):

| Aspek `PlayerMovement` Ibnu | Status vs setup M1-06 |
| --- | --- |
| MonoBehaviour + Unity Input System (`Keyboard.current`) | ✓ jalur input sama dengan test driver |
| `Rigidbody.MovePosition()` di `FixedUpdate` (XZ plane, WASD, diagonal dinormalisasi) | ✓ pola kinematik — NetworkTransform (Owner) mem-poll state tiap network tick; perubahan posisi owner langsung ter-replikasi tanpa custom replication |
| Script Ibnu belum punya guard owner (`IsOwner`) | ⚠ catatan integrasi tunggal — saat diintegrasikan, gerakan hanya boleh berjalan di instance owner (guard `IsOwner` di script movement atau wrapper tipis ala test driver). Bukan blocker NetworkTransform; tanpa guard, replica tetap "menang" (NT menulis ulang dari state network) tapi ada kerja/jitter sia-sia. |
| Rigidbody non-kinematic/gravity di masa depan | Opsi bawaan NGO `NetworkRigidbody` tersedia jika nanti dibutuhkan — tetap tanpa arsitektur custom. |

**Kesimpulan: YA — setup NetworkTransform saat ini dapat menerima movement seperti
`PlayerMovement` Ibnu tanpa membutuhkan custom networking architecture.**
Tidak ada kode tambahan yang dibuat untuk ini (sesuai prinsip jangan overengineer).

### Rencana Integrasi M1-05 (disepakati)

Ketika M1-05 selesai, kita **implement dari `PlayerMovement` Ibnu** — bukan membuat
movement system baru, bukan mempertahankan `TemporaryPlayerMovement`.
Checklist hari-H (verifikasi ulang saat eksekusi):

1. Bawa `PlayerMovement` ke production (branch/lokasi final ditentukan saat integrasi).
2. Production `Player.prefab` **belum punya Rigidbody** — `PlayerMovement` memanggil
   `GetComponent<Rigidbody>()` di `Awake` (NRE tanpa itu) → tambahkan Rigidbody di prefab.
3. Tambahkan **NetworkTransform** (Owner authority — konfigurasi yang sudah dibuktikan
   M1-06) ke production `Player.prefab`.
4. **Ownership gating**: `PlayerMovement` belum punya guard `IsOwner` (plain
   MonoBehaviour) → pilih saat integrasi: guard ditambahkan di script Ibnu (file Ibnu,
   keputusannya) atau gate tipis NetworkBehaviour di sisi integration. Tanpa gating,
   replica tetap dikoreksi NetworkTransform tiap tick (kerja sia-sia/jitter).
5. Setelah integrasi terverifikasi, hapus `TemporaryPlayerMovement` + folder test
   M1-06 ini.

## Checklist Verifikasi (apa yang diuji)

- **HOST**: `Player_0` bergerak dengan WASD; client melihat `Player_0` bergerak;
  client **tidak bisa** mengontrol `Player_0`.
- **CLIENT**: `Player_1` bergerak dengan WASD; host melihat `Player_1` bergerak;
  `Player_1` tetap dimiliki client yang benar (cek Inspector → NetworkObject → OwnerClientId).
- **MULTIPLAYER**: player lokal bergerak; player remote tidak merespons input lokal;
  posisi tersinkron via NetworkTransform; tidak ada duplicate player;
  spawning/despawning existing tetap bekerja.

## Status Verifikasi

| Item | Status |
| --- | --- |
| Kompilasi bersih (Unity CLI `recompile` → `recompile_status` failed=false) | PASS |
| Host runtime (CLI-verified): host start, spawn `Player_M106Test`, NetworkTransform(Owner) + driver terpasang di `Player_0` | PASS |
| MPPM: host `Player_0` bergerak dengan WASD & terlihat bergerak di client | PASS |
| MPPM: client `Player_1` bergerak dengan WASD & terlihat bergerak di host | PASS |
| MPPM: remote player tidak merespons input lokal (ownership guard) | PASS |
| MPPM: diagonal ternormalisasi (kecepatan diagonal == kecepatan lurus) | UNTESTED (runtime tidak diamati eksplisit; terverifikasi di kode: `move.normalized`) |
| MPPM: reconnect → ClientId ≥ 2, player lama despawn saat disconnect | PASS (log host: client 1 → client 2, despawn bersih) |
| MPPM: tidak ada duplicate player | PASS (satu player per client: `Player_0` owner=0, `Player_1` owner=1) |
| Regression MPPM ulang setelah final review | TIDAK DIULANG (keputusan: tidak ada perubahan kode/prefab sejak regression PASS — hanya README) |
| Physical LAN (di luar MPPM) | NOT TESTED (bukan target task ini) |

> PASS hanya boleh diklaim setelah observasi runtime aktual di MPPM.
> Loopback/MPPM PASS ≠ Physical LAN PASS.

## Hasil Verifikasi (2026-09-10)

- Setup temporary: `Player_M106Test` didaftarkan ke `DefaultNetworkPrefabs` dan di-set
  sebagai `PlayerPrefab` NetworkManager (`SampleScene`) via editor script. Setelah
  verifikasi selesai, `SampleScene.unity`, `DefaultNetworkPrefabs.asset`, dan
  `ProjectSettings/MultiplayerManager.asset` di-revert (`git restore`) — production
  tetap memakai prefab `Player` lama.
- **Host (CLI-verified)**: host start OK, `Player_0` spawn dengan NetworkTransform (Owner)
  + TemporaryPlayerMovement terpasang, `Player_0` berpindah posisi via WASD.
- **MPPM (manual)**: virtual player connect (`[HOST] Client connected: 1. Total clients: 2`,
  `Player spawned for client 1 (remote replica)`); sinkronisasi posisi **dua arah**
  (Player_0 ↔ Player_1) dikonfirmasi manual; remote player tidak merespons input lokal.
- **Reconnect regression**: client di-stop lalu Play lagi → `Client connected: 2`
  (ClientId 1→2, tidak dipakai ulang); player lama client pertama despawn bersih saat
  disconnect; host tetap stabil setelah disconnect.
- Diagonal normalization tidak diamati eksplisit saat sesi test (terverifikasi di kode:
  input dinormalisasi sebelum diterapkan), jadi tidak diklaim PASS runtime.
- **Final review (2026-09-10)**: tidak ada perubahan kode/prefab — hanya README
  (pemisahan tanggung jawab A/B/C + hasil compatibility M1-05). Compatibility check
  terhadap `PlayerMovement` M1-05 (commit `7138c9fe`, branch `programmer/mechanics`,
  read-only) = **COMPATIBLE**. Interpolasi/jitter tetap UNTESTED; regression MPPM
  tidak diulang karena tidak ada perubahan runtime sejak PASS.

## Batasan Prototype

- Tidak ada gravity/jump, tidak ada collision handling, tidak ada acceleration/deceleration,
  tidak ada sprint/stamina, tidak ada animation, tidak ada camera follow.
- Interpolasi memakai setting default NetworkTransform (tidak ada smoothing custom).
- `NetworkTransform.AuthorityMode` pada prefab variant di-set **Owner** untuk test
  ini (lihat [Keputusan Authority](#keputusan-authority-test-prefab-saja)).
  Script movement tetap mengakomodasi kedua mode.
- Tidak mengubah `NetworkSession.cs`, `PlayerSpawnHandler.cs`, `NetworkPlayerIdentity.cs`,
  atau arsitektur networking mana pun.
- Movement gameplay final **bukan** tanggung jawab M1-06: `TemporaryPlayerMovement`
  hanya test driver (lihat [Pemisahan Tanggung Jawab](#pemisahan-tanggung-jawab));
  jangan membangun gameplay di atas file ini.
- Production `Player.prefab` memiliki NetworkObject flag `Ownership: Distributable`
  (fitur redistribusi ownership NGO); assignment owner tetap dari spawn flow M1-03 —
  dicatat sebagai observasi, tidak diubah, tidak berkonflik dengan Owner authority.
