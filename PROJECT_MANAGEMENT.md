# PBJ — Project Management

Dokumen ini panduan singkat buat semua anggota tim: gimana cara kita pakai GitHub Project, Issues, dan status kerja. Targetnya simpel — kamu gak perlu jago GitHub, cukup tahu apa yang harus dilakuin saat dapat task.

---

## 1. GitHub Project

Board GitHub Project adalah tempat utama untuk lihat semua pekerjaan project dan statusnya:

> **Board = daftar semua pekerjaan project dan statusnya.**

Buka di sini: <https://github.com/users/Akabim/projects/3/views/1>

Di board kamu bisa lihat task apa saja yang ada, statusnya (belum dikerjakan, sedang dikerjakan, selesai, dll), dan siapa yang ngerjain.

<!-- Replace with the GitHub attachment URL after uploading the screenshot. -->
<img width="1914" height="879" alt="image" src="https://github.com/user-attachments/assets/0d8fea44-f3b0-42bf-a35f-8ac6e021812c" />


---

## 2. Issue

> **Issue = satu task/pekerjaan.**

Setiap pekerjaan di project ini dibikin sebagai Issue. Contoh judulnya:

```text
[M1-03] Network Player Spawning
```

Satu Issue biasanya berisi:

* **Tujuan** — kenapa task ini ada.
* **Pekerjaan** — apa saja yang harus dikerjakan.
* **Notes** — catatan tambahan, batasan scope.
* **Output** — hasil akhir yang diharapkan.
* **Acceptance Criteria** — checklist yang menentukan task dibilang selesai.

**Penting:** baca seluruh isi Issue dulu sebelum mulai kerja. Jangan asal gas — sering ada batasan scope yang ditulis di dalamnya.

<!-- Replace with the GitHub attachment URL after uploading the screenshot. -->
<img width="1127" height="901" alt="image" src="https://github.com/user-attachments/assets/507c66ca-36e9-41df-92a0-913d81be672d" />


---

## 3. Acceptance Criteria

Acceptance Criteria itu daftar centang yang menentukan task dianggap selesai atau belum. Contohnya:

```md
- [ ] Player memiliki NetworkObject
- [ ] Player spawn ketika client masuk
- [ ] Tidak ada duplicate player
```

Aturannya sederhana:

> Kalau bagian tersebut sudah **benar-benar selesai dan sudah dicek**, baru centang checkbox-nya.

Jadi bukan "kodenya udah ditulis" → langsung centang. Centang cuma kalau udah dites dan hasilnya sesuai. Kalau ada yang belum bisa dites, biarkan kosong dan tulis catatannya.

---

## 4. Status Workflow

Setiap Issue punya status di board dengan alur:

```text
BACKLOG
↓
READY
↓
IN PROGRESS
↓
REVIEW
↓
TESTING
↓
DONE
```

Artinya masing-masing:

* **BACKLOG** — udah dicatat, tapi belum siap dikerjakan.
* **READY** — siap dikerjakan, tinggal ambil.
* **IN PROGRESS** — lagi dikerjakan.
* **REVIEW** — pekerjaan selesai, nunggu review.
* **TESTING** — lagi diverifikasi/dites.
* **DONE** — selesai dan disetujui.

Buat anggota tim, alur yang perlu kamu pahami cuma ini:

```text
READY
↓
IN PROGRESS
↓
REVIEW
```

1. Ambil task yang **READY**.
2. Pindahin statusnya ke **IN PROGRESS**.
3. Kalau udah selesai + udah dites → pindahin ke **REVIEW**, lalu **tag Abim di grup** biar kerjaannya bisa direview.

Setelah itu:

```text
REVIEW
↓
TESTING
↓
DONE
```

Yang pindahin Issue ke **DONE** adalah **Abim**, setelah review/testing selesai. Kamu gak perlu pusing soal ini.

---

## 5. Cara Mulai Mengerjakan Task

```text
1. Buka Issue
2. Baca semua instruksi
3. Pindahkan status ke IN PROGRESS
4. Kerjakan task
5. Test hasilnya
6. Centang Acceptance Criteria yang benar-benar selesai
7. Pindahkan ke REVIEW
8. Tag Abim di grup
```

Gitu aja. Simpel.

---

## 6. Branch

> **Branch = tempat kerja masing-masing, supaya perubahan gak langsung mengganggu `main`.**

Branch yang sekarang ada:

```text
main
programmer/systems
programmer/mechanics
artist/nesya
artist/aul
artist/linda
```

Masing-masing punya "pemilik":

```text
programmer/systems   → Abim
programmer/mechanics → Ibnu
artist/nesya         → Nesya
artist/aul           → Aul
artist/linda         → Linda
```

Kerjakan di branch kamu sendiri, jangan kerja langsung di `main`.

---

## 7. Git Workflow Dasar

Buat programmer, alur harian di branch masing-masing:

```bash
git pull
```
Ambil perubahan terbaru dari GitHub.

*(kerja di Unity)*

```bash
git status
```
Cek file apa saja yang berubah.

```bash
git add .
git commit -m "deskripsi singkat"
git push
```

* `git add .` — masukin perubahan ke checkpoint.
* `git commit -m "..."` — simpan checkpoint.
* `git push` — kirim checkpoint ke GitHub.

Buat artist / UI / audio: cukup tahu ini aja, gak perlu belajar Git lebih jauh. Kalau macet, tanya Abim.

---

## 8. Playground vs `_Project`

Aturan folder di project ini:

```text
Assets/Playground/
```
= tempat prototype / experiment / kerjaan sementara.

```text
Assets/_Project/
```
= production / sistem yang dipakai bareng.

Simple rule:

> Kalau masih coba-coba → gunakan **Playground**.
> Kalau udah production/shared → baru masuk **_Project**.

Jangan langsung masukin eksperimen mentah ke `_Project`.

---

## 9. Kalau Bingung

Santai, semua pernah bingung. Yang penting **jangan ngasal**:

* Masalah Git? Jalankan:

```bash
git status
```

lalu **kirim hasilnya ke Abim** (screenshot/copy-paste).

* **JANGAN** sembarangan pakai:

```bash
git reset --hard
git push --force
```

* **JANGAN** hapus file cuma biar Git kelihatan "bersih" — itu malah bisa ngapus kerjaan.

Kalau ragu → berhenti → tanya Abim. Itu selalu langkah yang benar.

---

## 10. Contoh Workflow Lengkap

```text
Issue masuk
↓
READY
↓
Mulai kerja → IN PROGRESS
↓
Implementasi
↓
Test
↓
Acceptance Criteria selesai
↓
REVIEW
↓
Tag Abim
↓
Review / Testing
↓
DONE
```

---

## 11. Role Tim

| Anggota | Fokus                                            |
| ------- | ------------------------------------------------ |
| Abim    | Systems, Networking, PM, Integration, 3D support |
| Ibnu    | Gameplay Mechanics                               |
| Nesya   | Audio                                            |
| Aul     | UI/UX                                            |
| Linda   | 2D Art                                           |

---

## 12. Contoh Completion / Review

Kalau task selesai, hasilnya didokumentasikan sebagai **completion record** di Issue. Isinya kurang lebih:

* apa yang diubah;
* apa yang dites;
* apa yang PASS;
* apa yang belum dites;
* commit mana yang berisi pekerjaannya;
* keterbatasannya apa.

Jadi pas review, semua orang bisa lihat track record pekerjaannya — bukan cuma "udah kelar".

<!-- Replace with the GitHub attachment URL after uploading the screenshot. -->
<img width="1134" height="901" alt="image" src="https://github.com/user-attachments/assets/16589511-2def-47b3-b34c-2a8d567d1c66" />

---

## Link

* **Repository:** <https://github.com/Akabim/PBJ>
* **GitHub Project:** <https://github.com/users/Akabim/projects/3/views/1>
