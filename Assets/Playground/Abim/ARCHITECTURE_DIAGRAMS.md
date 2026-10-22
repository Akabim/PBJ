# Diagram Arsitektur Sistem Abim — PBJ

Dokumen ini merangkum sistem networking pada branch `programmer/systems`. Diagram membedakan implementasi yang sudah ada, prototype pengujian, dan rencana Relay agar statusnya tidak tertukar.

## 1. Fondasi Host/Client dan spawn pemain

Sumber utama: `Assets/_Project/Systems/Networking/NetworkSession.cs`, `Assets/_Project/Systems/Players/PlayerSpawnHandler.cs`, dan `Assets/_Project/Systems/Players/NetworkPlayerIdentity.cs`.

```mermaid
flowchart TD
    Host["Host memulai sesi<br/>H atau -pbjHost"] --> Session["NetworkSession"]
    Client["Client memulai sesi<br/>C atau -pbjClient [IP]"] --> Session
    Session --> Transport["UnityTransport<br/>alamat + port"]
    Transport <--> NGO["Netcode for GameObjects<br/>NetworkManager"]
    NGO -->|"ConnectionApprovalCallback<br/>(server)"| Spawn["PlayerSpawnHandler"]
    Spawn --> Validate["Validasi nama di server<br/>PlayerNameValidator"]
    Validate --> Player["Spawn Player NetworkObject<br/>posisi ditentukan server"]
    Player --> Identity["NetworkPlayerIdentity"]
    Identity --> Name["DisplayName NetworkVariable<br/>server menulis, semua client membaca"]
    Player --> Replicas["Player replica di host/client lain"]

    classDef current fill:#e6f4ea,stroke:#27834a,color:#123b20;
    class Host,Client,Session,Transport,NGO,Spawn,Validate,Player,Identity,Name,Replicas current;
```

Alur ini menggunakan Direct IP/LAN melalui Unity Transport. Server menyetujui koneksi dan membuat player. Nama player yang diterima melalui payload divalidasi di server sebelum disimpan pada `NetworkPlayerIdentity`.

## 2. Sinkronisasi movement dan camera pada prototype M2

Sumber utama: [M1-06 NetworkMovementTest README](M1-06_NetworkMovementTest/README.md) dan [M2 Multiplayer Integration README](M2_MultiplayerIntegration/README.md).

```mermaid
flowchart LR
    Input["Input lokal WASD"] --> Gate["M2NetworkInputGate<br/>hanya komponen owner aktif"]
    Gate --> Movement["PlayerMovement<br/>(script gameplay Ibnu)"]
    Gate --> Facing["PlayerFacing<br/>(script gameplay Ibnu)"]
    Movement --> Rigidbody["Rigidbody"]
    Rigidbody --> NTransform["NetworkTransform<br/>Owner authority"]
    Facing --> NTransform
    Rigidbody --> NBody["NetworkRigidbody"]
    NTransform --> Remote["Replica player di client lain<br/>posisi + rotasi tersinkron"]
    NBody --> Remote
    LocalPlayer["Player milik local client"] --> Binder["M2LocalCameraBinder"]
    Binder --> Camera["CameraFollow lokal<br/>tidak disinkronkan lewat network"]

    classDef test fill:#fff4d6,stroke:#b8860b,color:#4f3800;
    class Input,Gate,Movement,Facing,Rigidbody,NTransform,NBody,Remote,LocalPlayer,Binder,Camera test;
```

**Penting:** `M2_MultiplayerIntegration` adalah lingkungan uji, bukan scene production. `TemporaryPlayerMovement` di M1-06 juga hanya test driver sementara. Pada prototype M2, `M2NetworkInputGate` mencegah replica memproses input lokal; `M2LocalCameraBinder` menghubungkan kamera tiap instance ke player milik instance itu sendiri.

## 3. Alur pickup/drop item yang disinkronkan (M2-02)

Sumber utama: `M2NetworkedPickup.cs` dan bagian “M2-02 networked pickup/drop” di [README integrasi M2](M2_MultiplayerIntegration/README.md).

```mermaid
sequenceDiagram
    actor Owner as Player pemilik
    participant Gameplay as PlayerInteraction / PlayerInventory
    participant Item as M2NetworkedPickup
    participant Server as Host / server NGO
    participant State as carrierClientId
    participant Others as Client lain

    Owner->>Gameplay: Tekan E untuk pickup
    Gameplay->>Item: RequestPickup()
    alt request berasal dari host
        Item->>Server: Validasi langsung
    else request berasal dari client
        Item->>Server: PickupServerRpc()
    end
    Server->>Server: Validasi player, jarak, status item,<br/>dan apakah player sudah membawa item
    Server->>State: Set ID carrier
    Server->>Item: Parent ke player + offset lokal
    Server->>Item: Matikan collider dan NetworkTransform item
    State-->>Others: Status carrier direplikasi
    Item-->>Others: Parenting/visual mengikuti state NGO

    Owner->>Gameplay: Tekan Q untuk drop
    Gameplay->>Item: RequestDrop()
    alt request berasal dari host
        Item->>Server: Validasi carrier langsung
    else request berasal dari client
        Item->>Server: DropServerRpc()
    end
    Server->>Server: Pastikan pengirim adalah carrier
    Server->>Item: Lepas parent dan taruh item di depan player
    Server->>State: Set status Dropped
    Server->>Item: Aktifkan collider dan NetworkTransform
    State-->>Others: Status drop direplikasi
```

Aturan yang perlu dipertahankan:
- Server menjadi otoritas pickup/drop dan memvalidasi pengirim serta jarak.
- Item tetap server-owned. `carrierClientId` menyimpan siapa yang membawa item.
- Tidak memakai ClientRpc untuk visual; state network dan parenting NGO menyebarkan hasilnya.
- Saat item dibawa, NetworkTransform item dimatikan agar tidak melawan transform parent. Saat drop, komponen itu aktif kembali.

## 4. Online Relay (rencana M3, belum ditandai sebagai implementasi)

Sumber: [M3 Online Relay PLAN](M3_OnlineRelay/PLAN.md). Diagram ini adalah target yang direncanakan, bukan klaim bahwa integrasi Relay sudah selesai.

```mermaid
flowchart LR
    HostUI["Host: nama + Create Room"] --> OnlineSession["OnlineSession<br/>(direncanakan)"]
    ClientUI["Client: nama + join code"] --> OnlineSession
    OnlineSession --> UGS["Unity Authentication +<br/>Multiplayer Services"]
    UGS --> Relay["Unity Relay"]
    Relay --> Transport["UnityTransport"]
    Transport --> NGO["Netcode for GameObjects"]
    NGO --> Existing["Sistem PBJ yang sudah ada<br/>spawn, identity, gameplay M2"]

    classDef planned fill:#fce8e6,stroke:#c0392b,color:#5b1712,stroke-dasharray: 5 5;
    class HostUI,ClientUI,OnlineSession,UGS,Relay,Transport planned;
    class NGO,Existing fill:#e6f4ea,stroke:#27834a,color:#123b20;
```

Menurut audit yang dicatat di PLAN M3, integrasi OnlineSession/Relay memerlukan setup Unity Gaming Services dan package terkait. Pertahankan Direct IP/LAN yang ada sebagai jalur terpisah sampai Relay diuji.

## Status ringkas

| Bagian | Status pada dokumentasi repo |
|---|---|
| Direct IP/LAN via `NetworkSession` + UnityTransport | Implementasi ada |
| Server-authoritative approval/spawn + identity pemain | Implementasi ada |
| NetworkTransform movement proof M1-06 | Prototype test |
| Camera ownership gate dan network pickup/drop M2 | Prototype integrasi test |
| Online Relay melalui UGS Multiplayer Services | Rencana M3 di `PLAN.md` |

