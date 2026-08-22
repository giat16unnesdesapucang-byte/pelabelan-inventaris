# PROMPT & OUTLINE MATERI PRESENTASI POWERPOINT (PPT)
**SISTEM INFORMASI INVENTARISASI & PELABELAN ASET BALAI DESA PUCANG**
*Program Kerja Pengabdian Masyarakat - KKN GIAT 16 Universitas Negeri Semarang (UNNES) 2026*

---

> **CARA MENGGUNAKAN:**  
> Salin (*copy*) seluruh teks di bawah ini, lalu tempel (*paste*) ke dalam **Gemini AI** dengan instruksi pembuka:  
> *"Tolong buatkan draf presentasi PowerPoint (PPT) yang menarik, profesional, dan elegan untuk audiensi Perangkat Desa dan Dosen berdasarkan materi berikut ini:"*

---

```markdown
# BRIEFING & MATERI PRESENTASI SISTEM INVENTARISASI DESA PUCANG

## TEMA DESAIN & AESTHETICS YANG DIINGINKAN:
- **Warna Utama:** Hijau Zamrud/Emerald (#059669), Biru Dongker (#0F172A), Putih Bersih (#FFFFFF), Aksen Emas/Teal (#0F766E).
- **Karakter Visual:** Formal Pemerintahan Modern, Bersih, Berteknologi Tinggi, Mudah Dipahami oleh Perangkat Desa.
- **Audiens:** Kepala Desa Pucang, Sekretaris Desa, Perangkat Desa, Pengurus Barang Desa, dan Dosen Pembimbing Lapangan (DPL) UNNES.

---

### SLIDE 1: JUDUL UTAMA (TITLE SLIDE)
- **Judul:** DIGITALISASI & PENATAUSAHAAN ASET DESA BERBASIS QR CODE CLOUD
- **Subjudul:** Sistem Informasi Pelabelan, Pelacakan, dan Sirkulasi Inventaris Barang Milik Desa (BMD) Pucang
- **Presenter:** Tim Mahasiswa KKN GIAT 16 Universitas Negeri Semarang (UNNES)
- **Lokasi & Periode:** Desa Pucang, Kecamatan Secang, Kabupaten Magelang - Tahun 2026
- **Elemen Visual:** Logo Resmi UNNES GIAT 16 Desa Pucang 2026, ornamen balai desa modern, icon barcode/QR.

---

### SLIDE 2: LATAR BELAKANG & PERMASALAHAN
- **Poin 1 (Pencatatan Konvensional):** Inventarisasi desa sebelumnya masih menggunakan buku fisik/catatan manual yang rentan tercecer, rusak, atau tidak terperbarui secara berkala.
- **Poin 2 (Monitoring Peminjaman Barang):** Aset balai desa (kursi pertemuan, tenda, sound system, proyektor) sering dipinjam warga/organisasi tanpa pencatatan waktu kembali yang jelas, menyulitkan pelacakan barang.
- **Poin 3 (Tuntutan Akuntabilitas KIB):** Kebutuhan format laporan resmi Buku Inventaris Aset Desa (KIB) yang baku dan rapi untuk lampiran SPJ/LPJ desa sesuai Permendagri No. 1 Tahun 2016.

---

### SLIDE 3: SOLUSI INOVASI YANG DIBANGUN (THE SOLUTION)
- **Solusi Terintegrasi (3 Pilar Ekosistem):**
  1. **Aplikasi Komputer Balai Desa (Desktop Admin Windows):** Pusat manajemen data aset master, cetak stiker label QR massal, dan pembuatan laporan resmi Excel (.xlsx).
  2. **Aplikasi Smartphone Petugas (Mobile Scanner PWA):** Pemindai kamera QR di HP petugas untuk pencatatan pinjam/kembali di lapangan tanpa instalasi rumit.
  3. **Penyimpanan Cloud Real-Time (Supabase Database):** Sinkronisasi otomatis dua arah antara komputer kantor dan HP petugas di lapangan.
- **Metode Pelabelan:** Stiker QR Code tahan lama yang tertempel di setiap unit fisik barang desa.

---

### SLIDE 4: FITUR UNGGULAN APLIKASI DESKTOP ADMIN
- **1. Smart Auto-Generate SKU:** Pembuatan kode inventaris otomatis baku anti-duplikasi (`PCG-[KATEGORI]-[TAHUN]-[NO_URUT]`, contoh: `PCG-ELK-2026-001`).
- **2. Pendataan Komprehensif:** Mencatat Sumber Dana (DDS, ADD, PADes, Bankeu), Tanggal Perolehan, Nilai Aset (Rp), Kondisi Fisik, Lokasi Simpan, dan Foto Asli Barang.
- **3. Dual Live Preview:** Pratinjau langsung stiker QR dan foto fisik barang saat data dipilih.
- **4. Cetak Batch QR Label:** Mencetak lembaran stiker QR code siap tempel hanya dengan satu klik.

---

### SLIDE 5: LAPORAN RESMI EXCEL OTOMATIS (EXECUTIVE REPORTING)
- **A. Buku Inventarisasi Aset Desa (KIB):**
  - Dilengkapi KOP Surat Resmi Pemerintah Desa Pucang.
  - Kartu Ringkasan KPI Eksekutif (Total Unit, Total Nilai Aset Rp, Status, dan Kondisi).
  - Format akuntansi Rupiah otomatis (`Rp #,##0`) dan rumus penjumlahan `=SUM(...)`.
  - Kolom Tanda Tangan Pengesahan Kepala Desa Pucang & Pengurus Barang.
- **B. Laporan Sirkulasi Peminjaman Bulanan (SPJ/LPJ):**
  - Filter periode fleksibel (Laporan Bulanan, Rentang Tanggal Kustom, atau Semua Riwayat).

---

### SLIDE 6: FITUR UNGGULAN MOBILE SCANNER (APLIKASI HP)
- **1. Gerbang Keamanan PIN 1x:** Akses aman terlindungi PIN petugas (`123456`), hanya diminta 1x saat aktivasi HP pertama kali.
- **2. Kemudahan PWA (Progressive Web App):** Dapat dipasang langsung ke layar utama HP (*Add to Home Screen*) dengan logo resmi Desa Pucang 2026.
- **3. Pemindai Kamera Pintar:** Otomatis mendeteksi lensa belakang HP dan dilengkapi tombol *“🔄 Ganti Lensa Kamera”* untuk HP multi-kamera.
- **4. Layanan Cepat di Lokasi:** Pindai barcode QR dalam hitungan 0.5 detik langsung menampilkan data barang.

---

### SLIDE 7: ALUR KERJA SIRKULASI PEMINJAMAN BARANG (WORKFLOW)
- **Langkah 1 (Peminjaman):** Warga meminjam barang $\rightarrow$ Petugas scan QR $\rightarrow$ Input Nama, NIK KTP, No. WA, & Batas Kembali $\rightarrow$ Status berubah menjadi *“Dipinjam”*.
- **Langkah 2 (Monitoring):** Admin desktop dapat memantau barang yang sedang dibawa dan melihat peringatan jika ada barang yang jatuh tempo/terlambat.
- **Langkah 3 (Pengembalian):** Warga mengembalikan barang $\rightarrow$ Petugas scan QR $\rightarrow$ Klik *“Proses Pengembalian”* $\rightarrow$ Status seketika kembali *“Tersedia”*.

---

### SLIDE 8: DAMPAK & MANFAAT BAGI PEMERINTAH DESA PUCANG
- **1. Transparansi & Akuntabilitas:** Mengurangi risiko barang milik desa hilang atau tidak diketahui keberadaannya.
- **2. Efisiensi Waktu Kerja:** Pembuatan laporan aset desa yang semula berhari-hari kini selesai dalam hitungan detik.
- **3. Standarisasi Administrasi:** Tata kelola penomoran aset desa menjadi rapi, seragam, dan berstandar nasional.
- **4. Nol Biaya Pemeliharaan Server:** Menggunakan infrastruktur cloud gratis yang aman dan andal tanpa beban biaya server lokal desa.

---

### SLIDE 9: PAKET SERAH TERIMA & KEBERLANJUTAN SISTEM
- **Isi Paket Serah Terima KKN GIAT 16:**
  1. File Portable `DesktopAdmin.exe` siap pakai di komputer balai desa (tanpa perlu install).
  2. Buku Panduan Operasional Resmi format PDF dan cetak fisik jilid rapi.
  3. Lembar Pengesahan Serah Terima (Kepala Desa, DPL UNNES, KORMADES).
  4. Link Web Mobile PWA & Kredensial PIN Keamanan Petugas.
  5. Sesi Pelatihan & Pendampingan langsung kepada perangkat desa.

---

### SLIDE 10: PENUTUP & DEMONSTRASI LANGSUNG (LIVE DEMO)
- **Ucapan Terima Kasih:**
  - Pemerintah Desa Pucang, Kecamatan Secang, Kabupaten Magelang.
  - Pusat Pengembangan KKN LPPM Universitas Negeri Semarang (UNNES).
  - Seluruh Masyarakat Desa Pucang.
- **Ajakan Tindakan:** *"Mari Bersama Wujudkan Tata Kelola Aset Desa Pucang yang Modern, Akuntabel, dan Transparan."*
- **Sesi:** Demonstrasi Langsung Sistem & Tanya Jawab (Q&A).
```
