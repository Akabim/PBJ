# PBJ

Game multiplayer cooperative **Pick-up and Deliver** bertema **manusia purba**.
4 pemain bekerja sama: menerima daftar persembahan dari Shaman, mencari item di level, mengambilnya, lalu membawanya kembali ke Shaman.

Dibuat dengan **Unity 6.3 (6000.3.23f1)**, **Netcode for GameObjects**, dan **Unity Transport**.
Project ini masih dalam tahap pengembangan.

---

## Mulai Kerja dalam 5 Menit

Ikuti langkah berikut secara berurutan. Tidak perlu paham teorinya dulu.

### Langkah 1 — Terima invitation GitHub

Repository ini **private**, jadi kamu harus diundang dulu.

1. Cek email kamu. Cari email dari GitHub dengan judul seperti *"invitation to join Akabim/PBJ"*.
2. Klik tombol **Accept invitation** di email tersebut.
3. Kalau emailnya tidak muncul, buka langsung: https://github.com/Akabim/PBJ — kalau kamu sudah diundang, akan muncul tombol untuk menerima invitation.
4. Belum punya akun GitHub? Daftar dulu di https://github.com (gratis), lalu beri tahu username kamu ke Abim supaya diundang.

### Langkah 2 — Install Git (Windows)

1. Download Git di https://git-scm.com/download/win
2. Jalankan installer. **Klik Next terus** (pengaturan default sudah aman).
3. Setelah selesai, buka **Command Prompt** (tekan tombol Windows, ketik `cmd`, Enter) dan cek:

```
git --version
```

Kalau muncul nomor versi (contoh: `git version 2.47.0`), Git sudah siap.

### Langkah 2.5 — Login ke Git (hanya sekali, di awal)

Ini penting supaya Git tahu **siapa kamu** dan **punya izin mengakses repository tim**.

**A. Kenalkan nama dan email kamu ke Git** (sekali saja per laptop):

Di Command Prompt, ketik (ganti dengan nama dan email kamu sendiri):

```
git config --global user.name "Nama Kamu"
git config --global user.email "email-github-kamu@example.com"
```

Catatan: pakai **email yang kamu pakai untuk daftar GitHub**. Ini bukan login, cuma identitas yang tampil di riwayat pekerjaan.

**B. Login ke akun GitHub:**

Saat kamu nanti menjalankan `git push` untuk **pertama kali**, biasanya akan muncul jendela popup **"GitHub Sign in"** atau **"Git Credential Manager"**:

1. Pilih **Sign in with your browser** (atau klik **Sign in with GitHub**).
2. Browser akan terbuka → klik **Authorize**.
3. Selesai. Setelah itu Git tidak akan menanyakan login lagi.

Kalau popup tidak muncul dan kamu diminta memasukkan username/password di Command Prompt:
* Username → isi username GitHub kamu.
* Password → **JANGAN isi password GitHub kamu** (Git tidak akan terima). Kalau ini terjadi, berhenti dan tanya Abim — cara login yang benar adalah lewat browser seperti di atas.

Cara lain yang lebih gampang: install **GitHub CLI** lalu ketik:

```
gh auth login
```

ikuti petunjuknya (pilih `GitHub.com` → `Login with a web browser` → buka link yang muncul di browser → masukkan kode yang ditampilkan).

Kalau bingung, screenshot-kan ke Abim. Ini langkah yang paling sering bikin bingung pemula — kamu tidak sendirian. 😄

### Langkah 3 — Clone repository

Clone = **mengambil project dari GitHub ke laptop kamu**.

Di Command Prompt, pindah ke folder tempat project Unity kamu biasa disimpan, lalu clone:

```
git clone https://github.com/Akabim/PBJ.git
cd PBJ
```

Sekarang ada folder `PBJ` berisi seluruh project di laptop kamu.

### Langkah 4 — Buka Unity

1. Buka Unity Hub.
2. Open → pilih folder project `PBJ` tadi.
3. **WAJIB pakai Unity 6000.3.23f1.** Jangan pakai versi Unity yang berbeda — versi berbeda bisa merusak project.

Tunggu Unity selesai import. Ini bisa agak lama di awal, itu normal.

### Langkah 5 — Masuk ke branch kamu

Branch masing-masing anggota **sudah dibuat**. Kamu **tidak perlu** membuat branch baru.

Di Command Prompt (di dalam folder `PBJ`), ketik sesuai nama kamu:

| Nama | Command |
|---|---|
| Abim | `git switch programmer/systems` |
| Ibnu | `git switch programmer/mechanics` |
| Nesya | `git switch artist/nesya` |
| Aul | `git switch artist/aul` |
| Linda | `git switch artist/linda` |

Branch = **tempat kerja kamu**. Selama kamu bekerja di branch kamu, pekerjaanmu tidak langsung mengganggu `main` (branch bersama tim).

Selesai! Kamu sudah siap kerja. Lanjut ke bagian "Workflow Sehari-hari" di bawah untuk rutinitas harian.

---

## Tenang, Kalian Tidak Perlu Jago Git

Cukup paham 6 kata ini:

* **Git** — menyimpan riwayat semua pekerjaan. Seperti save game, tapi setiap save bisa dibuka lagi.
* **GitHub** — tempat online tempat repository tim disimpan.
* **Branch** — jalur kerja masing-masing. Kayak jalan tol punya lajur sendiri.
* **Commit** — menyimpan checkpoint pekerjaan. Kayak save point.
* **Push** — mengirim checkpoint ke GitHub.
* **Pull** — mengambil perubahan terbaru dari GitHub.

Itu saja. Sisanya cuma mengetik beberapa command yang ada di panduan ini.


---

## Workflow Sehari-hari

Rutinitas setiap hari kerja, dari mulai sampai selesai:

```
git pull          ← ambil perubahan terbaru
   ↓
kerja di Unity    ← kerjakan tugasmu
   ↓
git status        ← cek apa yang berubah
   ↓
git add .         ← masukkan perubahan ke checkpoint
   ↓
git commit -m "deskripsi"   ← simpan checkpoint
   ↓
git push          ← kirim checkpoint ke GitHub
```

Fungsi tiap command (satu kalimat):

* `git pull` — ambil perubahan terbaru dari repository.
* `git status` — cek apa saja yang berubah.
* `git add .` — masukkan perubahan ke checkpoint.
* `git commit -m "..."` — simpan checkpoint.
* `git push` — kirim checkpoint ke GitHub.

---

## Contoh: Saya Mau Mulai Kerja Hari Ini

**Contoh untuk artist (Nesya):**

```
cd PBJ
git switch artist/nesya
git pull
```

Kerjakan pekerjaan di Unity, lalu kalau satu bagian pekerjaan selesai:

```
git status
git add .
git commit -m "art: tambah aset karakter"
git push
```

Selesai. Setelah push, hasil kerjamu **sudah ada di GitHub** dan aman.

**Contoh untuk programmer:**

Abim:

```
git switch programmer/systems
git pull
```

Ibnu:

```
git switch programmer/mechanics
git pull
```

Lalu kerja, commit, push — persis seperti contoh Nesya di atas.

---

## Cara Tahu Saya Sedang di Branch Mana?

```
git branch --show-current
```

Contoh hasilnya:

```
programmer/mechanics
```

Kalau output-nya sudah nama branch kamu, berarti kamu sudah di tempat yang benar. Lanjut kerja.

---

## Cara Ambil Update Tim

```
git pull
```

Biasakan menjalankan `git pull` **sebelum mulai kerja** supaya kamu dapat file terbaru dari tim.

Satu aturan sederhana: kalau kamu sudah mengubah banyak file tapi belum commit, **simpan pekerjaanmu dulu** (commit) sebelum pull.

---

## Cara Menyimpan Pekerjaan

```
git status
git add .
git commit -m "deskripsi pekerjaan"
git push
```

Untuk `commit -m`, tulis deskripsi singkat yang jelas. Contoh yang bagus:

* `feat: tambah interaksi pemain`
* `feat: tambah pickup item`
* `fix: perbaiki movement`
* `art: tambah asset shaman`
* `docs: update README`

Tidak perlu commit setiap 5 menit. Commit ketika **satu bagian pekerjaan yang masuk akal** selesai — misalnya satu script selesai, atau satu aset selesai dibuat.

---

## Pull Request

Alurnya sederhana:

```
Branch → Push → Pull Request → Review → Merge ke main
```

Kamu tidak perlu paham semua detail GitHub. Fokusnya satu:

> Kalau tugasmu sudah **selesai dan siap digabung ke main**, buat **Pull Request**.

Caranya: setelah push, buka https://github.com/Akabim/PBJ — biasanya GitHub menampilkan tombol **"Compare & pull request"**. Klik itu, tulis penjelasan singkat apa yang kamu kerjakan, lalu klik **Create pull request**. Abim atau tim akan review sebelum digabung.

---

## Kalau Takut Salah / Bingung

Baca ini baik-baik:

* **JANGAN panik.** Hampir semua kesalahan Git bisa diperbaiki.
* **JANGAN** pakai command berikut **tanpa instruksi dari Abim**:
  * `git reset --hard`
  * `git push --force`

Kalau ragu atau ada error yang tidak kamu pahami:

1. **Berhenti.** Jangan coba-coba command lain.
2. Jalankan `git status` dan screenshot hasilnya.
3. Tanyakan ke Abim.

---

## Conflict

Conflict terjadi ketika **dua orang mengubah bagian file yang sama**. Ini **masalah normal** di Git dan selalu bisa diselesaikan.

Kalau kamu melihat kata "CONFLICT" atau "merge conflict":

* **JANGAN** asal pilih "ours" atau "theirs".
* **JANGAN** menghapus file.
* **JANGAN** reset.

Cukup: screenshot atau copy output conflict-nya, lalu kirim ke Abim.

---

## Aturan Unity

Semua anggota menggunakan **Unity 6000.3.23f1**.

Aturan penting:

* Gunakan **versi Unity yang sama** dengan tim (6000.3.23f1).
* **Jangan install package sendiri** tanpa koordinasi. Butuh package? Diskusi dulu.
* **Jangan menghapus file `.meta`** — file kecil yang menemani tiap aset Unity. File itu penting.
* **Jangan sembarang mengedit scene shared** (di `Assets/Scenes/`) tanpa koordinasi — dua orang mengedit scene yang sama bersamaan = conflict pusing.
* Eksperimen → di `Playground`. Kode/aset yang sudah matang → `_Project`.

---

## Struktur Folder

```
Assets/
├── _Project/      ← production (kode & aset resmi untuk game)
├── Playground/    ← eksperimen pribadi masing-masing anggota
├── Art/           ← art bersama
├── Prefabs/       ← prefab bersama
└── Scenes/        ← scene bersama
```

Playground masing-masing anggota:

```
Assets/Playground/Abim/
Assets/Playground/Ibnu/
Assets/Playground/Nesya/
Assets/Playground/Aul/
Assets/Playground/Linda/
```

---

## Playground vs _Project

Analogi sederhana:

* **Playground** = meja kerja / tempat coba-coba. Bebas berantakan.
* **_Project** = barang yang sudah cukup matang untuk dipakai tim. Harus rapi.

Contoh: kalau Ibnu sedang mencoba-coba movement:

```
Assets/Playground/Ibnu/
```

Kalau movement-nya sudah jalan dengan baik dan disetujui jadi production:

```
Assets/_Project/Systems/Players/
```

**Jangan langsung memasukkan eksperimen mentah ke `_Project`.** Selesaikan dulu di Playground, baru dipindahkan saat sudah matang.

---

## Scene

`Assets/Scenes/` adalah **shared** (dipakai bersama). Saat ini berisi `SampleScene`.

* Jangan dua orang mengedit scene yang sama secara bersamaan tanpa koordinasi.
* Eksperimen pribadi → buat scene sendiri di `Playground`.

---

## Package

Yang sudah terpasang dan **tidak perlu diutak-atik**:

* **Netcode for GameObjects (NGO)** — sudah terpasang. Jangan install lagi.
* **Unity Transport** — sudah terpasang.

Jangan mengubah versi package tanpa diskusi tim. Kalau ada package yang butuh di-install, koordinasikan dulu.

---

## File yang Tidak Perlu Dipikirkan

Folder seperti ini:

```
Library/  Temp/  Obj/  Build/  Builds/  Logs/  UserSettings/
```

sudah otomatis diabaikan oleh `.gitignore`. Kamu **tidak perlu mengurusnya** — jangan di-commit, jangan dikirim ke teman, biarkan saja.

---

## PROJECT.md

`PROJECT.md` adalah file context **lokal milik Abim** untuk tool AI (Cline). Ini **bukan dokumentasi tim** dan **tidak masuk repository**.

Anggota lain **tidak perlu membuat file itu**. Dokumentasi bersama ada di **README.md** ini.

---

## Kalau Git Bermasalah

Kirim ke Abim:

1. Command yang kamu jalankan.
2. Output / pesan error lengkapnya.
3. Screenshot jika perlu.

Contoh pesan yang bagus:

> "Abim, aku tadi jalankan `git push` dan muncul error ini: [paste error]"

Jangan hanya bilang: *"Gitku error."* 😅

---

## Branch Tim

| Branch | Owner |
|---|---|
| `main` | bersama (stable — digabung lewat Pull Request) |
| `programmer/systems` | Abim (Systems / Networking) |
| `programmer/mechanics` | Ibnu (Gameplay Mechanics) |
| `artist/nesya` | Nesya (Artist) |
| `artist/aul` | Aul (Artist) |
| `artist/linda` | Linda (Artist) |

---

## Roadmap Singkat

* **M0 — Foundation** ✅ (selesai)
* **M1 — Multiplayer Foundation** ← tahap berikutnya
* M2 — Core Pick-up
* M3 — Deliver
* M4 — Multiplayer Gameplay
* M5 — Art Integration
* M6 — Final

---

## Quick Reference (Cheat Sheet)

**Mulai kerja:**

```
git switch <branch-kamu>
git pull
```

**Selesai kerja:**

```
git status
git add .
git commit -m "deskripsi"
git push
```

**Cek sedang di branch mana:**

```
git branch --show-current
```

**Lihat kondisi (ada perubahan apa):**

```
git status
```

**Lihat semua branch:**

```
git branch -a
```

