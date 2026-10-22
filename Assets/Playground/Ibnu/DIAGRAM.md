# Diagram Sistem Gameplay — Ibnu

Diagram ini dibuat dari script dan scene yang ada di branch `programmer/mechanics`. Ini menjelaskan hubungan sistem berdasarkan kode; bukan klaim bahwa semua alur sudah diuji runtime.

## 1. Loop gameplay utama

```mermaid
flowchart TD
    Input["Keyboard Input"] --> Move["PlayerMovement"]
    Move --> Speed["Hitung kecepatan<br/>normal / sprint / bawa gentong berisi"]
    Speed --> RB["Rigidbody"]
    Input --> Stamina["Update stamina"]
    Stamina --> StaminaUI["StaminaBar"]
    RB --> World["Gerak pemain di scene"]

    Hunter["HunterChase<br/>Patrol → Chase → Windup → Recover"] --> Hit["PlayerDeath.TakeHit()"]
    Hit --> HP["HitsTaken / HitsRemaining"]
    HP --> HealthUI["HealthUIController<br/>sprite health"]
    Hit -->|jumlah hit mencapai batas| Dead["PlayerDeath.Die()"]
    Dead --> Ash["AshPile dibuat"]
    Ash --> Urn["Masukkan abu ke gentong kosong"]
    Urn --> Filled["Urn berisi abu"]
    Filled --> Altar["AltarRevive mendeteksi gentong berisi<br/>dalam radius altar"]
    Altar --> Revive["Urn dikonsumsi → PlayerDeath.Revive()"]
    Revive --> Protection["Perlindungan revive sementara"]

    classDef gameplay fill:#e5f0ff,stroke:#3267a8,color:#102b4d;
    classDef ui fill:#e9f7ec,stroke:#27834a,color:#123b20;
    class Input,Move,Speed,RB,World,Hunter,Hit,HP,Dead,Ash,Urn,Filled,Altar,Revive,Protection gameplay;
    class Stamina,StaminaUI,HealthUI ui;
```

## 2. Interaksi dan inventory

```mermaid
flowchart LR
    Player["Player"] --> Detect["PlayerInteraction<br/>mencari InteractableObject dalam jangkauan"]
    Detect --> Prompt["GameMessage.SetPrompt()"]
    Detect -->|"E"| Interact["InteractableObject.Interact()"]
    Interact --> Pickup["PickupItem"]
    Pickup --> Inventory["PlayerInventory.PickUp()"]
    Inventory --> Hold["HoldPoint / item dipegang"]
    Inventory -->|"Q"| Drop["PlayerInventory.Drop()"]
    Drop --> World["Item kembali ke dunia"]
    Interact --> Ash["AshPile"]
    Ash --> EmptyUrn["Urn kosong di tangan"]
    EmptyUrn --> Fill["Urn.Fill(player yang mati)"]
    Fill --> Altar["AltarRevive"]

    classDef systems fill:#f4edff,stroke:#7450a8,color:#30204d;
    class Player,Detect,Prompt,Interact,Pickup,Inventory,Hold,Drop,World,Ash,EmptyUrn,Fill,Altar systems;
```

## 3. Peta scene dan UI

```mermaid
flowchart TB
    Main["Assets/Playground/Ibnu/1.unity<br/>scene gameplay"] --> Gameplay["Hunter, player, altar,<br/>NavMesh, objective, health & stamina"]
    Test["Assets/Scenes/PBJ_UI_Integration_Test.unity"] --> MainMenu["MainMenuController<br/>panel Main / Host / Join"]
    MainMenu --> Hover["MenuButtonHover"]
    Lobby["Assets/Playground/Ibnu/Scene/WaitingRoom.unity"] --> LobbyUI["Layout lobby: slot pemain,<br/>nama pemain, kode room"]
    Gameplay --> Health["HealthUIController"]
    Gameplay --> Stamina["StaminaBar"]

    classDef scene fill:#fff4d6,stroke:#b8860b,color:#4f3800;
    class Main,Test,Lobby,Gameplay,MainMenu,Hover,LobbyUI,Health,Stamina scene;
```

Catatan: `WaitingRoom.unity` menunjukkan layout UI di branch mechanics. Integrasi sesi/network multiplayer berada pada branch `programmer/systems`; diagram ini tidak menganggap layout scene tersebut sudah terhubung ke session networking.

## Script utama

- `HunterChase.cs`: patroli, mengejar, windup serangan, recover.
- `PlayerDeath.cs`: hit, mati, sembunyi renderer/collider, revive, proteksi setelah revive.
- `AshPile.cs`, `Urn.cs`, `AltarRevive.cs`: alur abu sampai revive.
- `PlayerInteraction.cs`, `InteractableObject.cs`, `Pickupitem.cs`, `PlayerInventory.cs`: interaksi, pickup, bawa, dan drop.
- `PlayerMovement.cs`, `StaminaBar.cs`: gerak, sprint, stamina, dan indikator.
- `HealthUIController.cs`: memperbarui sprite health berdasarkan hit yang tersisa.
- `MainMenuController.cs`, `MenuButtonHover.cs`: navigasi panel dan animasi hover.
- `RandomSpawner.cs`, `BlockoutBuilder.cs`: penempatan item/objek dan pembuatan blockout level.
