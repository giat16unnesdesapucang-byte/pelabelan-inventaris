# BUKU PANDUAN PENGGUNAAN SISTEM INFORMASI INVENTARISASI & PELABELAN ASET DESA
**Balai Desa Pucang, Kecamatan Secang, Kabupaten Magelang**  
*Program Pengabdian Masyarakat - KKN GIAT 16 Universitas Negeri Semarang (UNNES) 2026*

---

## 📑 DAFTAR ISI
1. [BAB I: PENGENALAN SISTEM](#bab-i-pengenalan-sistem)
2. [BAB II: PANDUAN APLIKASI DESKTOP ADMIN](#bab-ii-panduan-aplikasi-desktop-admin)
   - 2.1 Menjalankan Aplikasi Desktop
   - 2.2 Membaca Dashboard & Statistik
   - 2.3 Menambah Aset Baru (Fitur Smart Auto-SKU)
   - 2.4 Mengubah & Menghapus Data Aset
   - 2.5 Mencetak Stiker Label QR Code (Batch Printing)
   - 2.6 Ekspor Laporan Resmi Excel (.xlsx) Standar Desa
   - 2.7 Mengatur PIN Keamanan Petugas Mobile
3. [BAB III: PANDUAN APLIKASI MOBILE SCANNER (PWA)](#bab-iii-panduan-aplikasi-mobile-scanner-pwa)
   - 3.1 Membuka Aplikasi di Smartphone
   - 3.2 Aktivasi PIN Perangkat (Hanya 1x)
   - 3.3 Memasang Aplikasi ke Layar Utama HP (Install PWA)
   - 3.4 Memindai QR Code Aset (Fitur Ganti Lensa)
   - 3.5 Mencatat Peminjaman Barang oleh Warga
   - 3.6 Memproses Pengembalian Barang
   - 3.7 Melihat Riwayat Peminjaman
4. [BAB IV: PERAWATAN SISTEM & TROUBLESHOOTING](#bab-iv-perawatan-sistem--troubleshooting)
5. [LEMBAR PENGESAHAN & SERAH TERIMA](#lembar-pengesahan--serah-terima)

---

## BAB I: PENGENALAN SISTEM

Sistem Informasi Inventarisasi Aset Desa Pucang adalah solusi digital modern terintegrasi yang dirancang untuk menatausahakan, melacak, dan mengamankan seluruh Barang Milik Desa (BMD) secara akuntabel dan transparan.

### 🌟 Arsitektur & Komponen Sistem:
1. **Desktop Admin Panel (Windows App):** Digunakan oleh Sekretaris Desa / Pengurus Barang di kantor balai desa untuk pendataan master aset, pencetakan stiker barcode QR, monitoring sirkulasi, dan penerbitan buku laporan inventaris resmi format Microsoft Excel (`.xlsx`).
2. **Mobile Scanner App (PWA):** Digunakan oleh Petugas Lapangan / Perangkat Desa melalui smartphone untuk memindai stiker QR pada fisik barang, melayani peminjaman warga secara instan di lokasi, dan memproses pengembalian.
3. **Database Cloud Real-Time (Supabase PostgreSQL):** Menghubungkan aplikasi Desktop dan HP secara langsung melalui cloud tanpa perlu server lokal.

---

## BAB II: PANDUAN APLIKASI DESKTOP ADMIN

### 2.1 Menjalankan Aplikasi Desktop
1. Buka File Explorer di laptop/komputer balai desa.
2. Masuk ke folder aplikasi `desktop-admin/bin/Release/net9.0-windows/win-x64/publish/`.
3. Klik 2x pada file **`DesktopAdmin.exe`**.
4. Aplikasi akan langsung terbuka dan otomatis terhubung ke database cloud.

### 2.2 Membaca Dashboard & Statistik
* **Total Aset Keseluruhan:** Jumlah unit barang fisik yang terdata.
* **Aset Tersedia:** Barang yang siap dipinjamkan atau digunakan di balai desa.
* **Sedang Dipinjam:** Barang yang saat ini dibawa oleh warga/pihak luar.
* **Butuh Perbaikan:** Barang yang berkondisi Rusak Ringan / Rusak Berat.
* **Peringatan Jatuh Tempo:** Menampilkan daftar peminjam yang terlambat mengembalikan barang.

### 2.3 Menambah Aset Baru (Fitur Smart Auto-SKU)
1. Masuk ke menu **Data Aset** di sidebar kiri.
2. Klik tombol **"+ Tambah Aset"** di toolbar atas.
3. Isi data barang:
   * **Nama Aset:** Nama barang lengkap (contoh: *Proyektor Epson EB-X400*).
   * **Kategori Aset:** Pilih kategori (contoh: *Elektronik*).
   * **Sumber Dana:** Pilih sumber anggaran (*Dana Desa (DDS), ADD, PADes, Bantuan Keuangan Provinsi/Kabupaten, Hibah*).
   * **Tanggal Perolehan:** Tentukan tanggal pembelian barang.
   * **Kode SKU / ID Aset:** Klik tombol **"⚡ Buat Kode Otomatis"** untuk membuat kode baku otomatis (*contoh: `PCG-ELK-2026-001`*).
   * **Lokasi Penyimpanan:** Ruang simpan barang (contoh: *Ruang Sekdes*).
   * **Nilai Aset (Rp):** Harga perolehan barang (contoh: *8500000*).
   * **Kondisi:** Baik, Rusak Ringan, atau Rusak Berat.
   * **Foto Fisik Aset:** Klik **"Pilih Foto"** untuk mengunggah foto asli barang (otomatis dikompresi hemat kuota).
   * **Deskripsi:** Catatan spesifikasi teknis barang.
4. Klik **"Simpan Aset"**.

### 2.4 Mengubah & Menghapus Data Aset
* **Mengubah Data:** Pilih salah satu aset di tabel $\rightarrow$ klik tombol **"Ubah"** di toolbar $\rightarrow$ sesuaikan data $\rightarrow$ klik **"Simpan"**.
* **Menghapus Data:** Pilih salah satu aset $\rightarrow$ klik tombol **"Hapus"** $\rightarrow$ konfirmasi **Yes**.

### 2.5 Mencetak Stiker Label QR Code (Batch Printing)
1. Di menu **Data Aset**, pilih aset yang ingin dicetak:
   * *Mencetak 1 barang:* Klik pada baris barang tersebut.
   * *Mencetak beberapa barang:* Tahan tombol `Ctrl` lalu klik beberapa baris barang.
   * *Mencetak seluruh barang:* Tidak perlu memilih baris, langsung klik tombol Cetak.
2. Klik tombol **"Cetak Batch QR"**.
3. Jendela pratinjau cetak akan terbuka menampilkan stiker siap cetak lengkap dengan Logo Desa Pucang, Kode SKU, Nama Barang, dan QR Code.
4. Klik **"🖨️ Cetak ke Printer"** untuk langsung mencetak ke kertas stiker label.

### 2.6 Ekspor Laporan Resmi Excel (.xlsx) Standar Desa
Aplikasi menyediakan 2 jenis dokumen laporan resmi Microsoft Excel murni:

#### A. Buku Inventarisasi Aset Desa (KIB)
* Klik tombol **"📊 Export Laporan Excel (.xlsx)"** di halaman Data Aset.
* Simpan file $\rightarrow$ Buka di Microsoft Excel.
* **Format Dokumen Memuat:**
  * KOP Surat Resmi Pemerintah Desa Pucang.
  * Kartu Ringkasan Eksekutif (Total Unit, Total Nilai Aset Rp, Status, dan Kondisi).
  * Format mata uang Rupiah otomatis (`Rp #,##0`) dan tanggal baku.
  * Formula Total Nilai Kekayaan Desa `=SUM(...)`.
  * Lembar Tanda Tangan Pengesahan Kepala Desa Pucang dan Pengurus Barang.

#### B. Laporan Rekapitulasi Sirkulasi Peminjaman (Bulanan / LPJ)
* Masuk ke menu **Sirkulasi Peminjaman** $\rightarrow$ klik **"📊 Export Laporan Peminjaman (.xlsx)"**.
* Muncul dialog pilihan periode:
  * **Laporan Bulanan (Default):** Pilih bulan dan tahun (contoh: *Agustus 2026*).
  * **Rentang Tanggal Kustom:** Tentukan tanggal awal dan akhir secara bebas.
  * **Semua Riwayat:** Untuk arsip seluruh transaksi sepanjang masa.
* Klik **"📥 Export Excel (.xlsx)"** $\rightarrow$ Dokumen Excel resmi periode tersebut siap dilampirkan ke SPJ/LPJ desa.

### 2.7 Mengatur PIN Keamanan Petugas Mobile
1. Klik menu **"⚙️ PIN Petugas Mobile"** di sidebar bawah.
2. Jendela pengaturan akan menampilkan PIN yang aktif saat ini.
3. Masukkan PIN Baru (4 hingga 8 digit angka, contoh: `260821`).
4. Konfirmasi PIN Baru $\rightarrow$ klik **"💾 Simpan PIN Baru"**.
5. PIN akan langsung terupdate secara real-time di server cloud.

---

## BAB III: PANDUAN APLIKASI MOBILE SCANNER (PWA)

### 3.1 Membuka Aplikasi di Smartphone
* Buka browser di smartphone (Google Chrome di Android atau Safari di iPhone).
* Masukkan tautan web Vercel resmi desa: `https://[nama-project].vercel.app`.

### 3.2 Aktivasi PIN Perangkat (Hanya 1x Seumur Hidup)
1. Saat dibuka pertama kali, layar akan terkunci menampilkan **"🛡️ Aktivasi Perangkat Petugas"**.
2. Masukkan PIN Keamanan Petugas (Default: **`123456`** atau PIN yang diatur admin).
3. Klik **"Aktifkan Smartphone Ini ➔"**.
4. Smartphone Anda kini resmi terdaftar. Anda **tidak perlu memasukkan PIN lagi** untuk penggunaan seterusnya!

### 3.3 Memasang Aplikasi ke Layar Utama HP (Install PWA)
1. Setelah aktivasi berhasil, akan muncul banner hijau **"📲 Pasang Aplikasi Aset Desa"** di bagian atas layar.
2. Klik tombol **"Pasang"** $\rightarrow$ pilih **"Install / Tambahkan"**.
3. Ikon aplikasi **Inventaris Pucang** kini muncul di layar utama smartphone Anda seperti aplikasi PlayStore/AppStore.
4. Buka aplikasi dari layar utama HP untuk menikmati tampilan layar penuh (*fullscreen*) tanpa address bar browser.

### 3.4 Memindai QR Code Aset
1. Ketuk ikon pemindai bulat hijau **[ ⛶ ]** di bilah menu bawah.
2. Kamera belakang smartphone akan otomatis aktif seketika.
3. Arahkan kotak pembidik ke stiker QR yang tertempel di barang inventaris.
4. *(Opsional)* Jika smartphone memiliki beberapa lensa, ketuk tombol **"🔄 Ganti Lensa Kamera"** untuk beralih lensa jika diperlukan.
5. Dalam 0.5 detik, sistem akan bergetar dan langsung membuka halaman detail aset yang dipindai!

### 3.5 Mencatat Peminjaman Barang oleh Warga
1. Pindai stiker QR barang $\rightarrow$ halaman Detail Aset terbuka.
2. Jika status barang *"Tersedia"*, ketuk tombol **"📦 Pinjamkan Aset Ini"**.
3. Isi formulir peminjaman:
   * **Nama Peminjam:** Nama lengkap warga peminjam.
   * **NIK Peminjam:** Nomor KTP warga (16 digit).
   * **No. Telepon / WhatsApp:** Nomor kontak aktif.
   * **Rencana Pengembalian:** Tanggal batas kembali barang.
4. Ketuk **"Konfirmasi Peminjaman"**.
5. Status barang seketika berubah menjadi *"Dipinjam"* di HP dan komputer Desktop Balai Desa!

### 3.6 Memproses Pengembalian Barang
1. Saat warga mengembalikan barang ke balai desa, pindai stiker QR barang tersebut.
2. Sistem menampilkan informasi siapa yang meminjam dan batas waktunya.
3. Ketuk tombol hijau **"↩️ Proses Pengembalian"**.
4. Konfirmasi pengembalian $\rightarrow$ Status barang langsung kembali *"Tersedia"*.

---

## BAB IV: PERAWATAN SISTEM & TROUBLESHOOTING

| Kendala / Masalah | Penyebab | Solusi Penanganan |
| :--- | :--- | :--- |
| **Kamera HP tidak menyala saat scan** | Izin kamera di browser belum diaktifkan | Buka Pengaturan Browser HP $\rightarrow$ Izin Situs $\rightarrow$ Kamera $\rightarrow$ Pilih *"Izinkan"*. |
| **Kamera HP membuka kamera depan** | Indeks multi-kamera default | Ketuk tombol **"🔄 Ganti Lensa Kamera"** di bawah kotak scan. |
| **Lupa PIN Petugas Mobile** | PIN diubah sebelumnya | Buka aplikasi Desktop Admin di kantor desa $\rightarrow$ klik menu *"⚙️ PIN Petugas Mobile"* untuk melihat & mereset PIN. |
| **Aplikasi HP ingin dikunci kembali** | HP dipindahtangankan | Di Beranda HP, ketuk badge hijau **"🛡️ Terotorisasi"** di pojok kanan atas $\rightarrow$ pilih *"OK / Kunci"*. |
| **Laptop tidak terhubung internet** | Jaringan WiFi/LAN terputus | Pastikan laptop balai desa terhubung ke internet agar data tersinkronisasi ke cloud Supabase. |

---

## LEMBAR PENGESAHAN & SERAH TERIMA

Dokumen ini disusun dan diserahterimakan sebagai bagian dari luaran program kerja **Digitalisasi & Pelabelan Inventarisasi Aset Balai Desa** oleh Tim Mahasiswa KKN GIAT 16 Universitas Negeri Semarang.

Ditetapkan di : **Desa Pucang**  
Pada tanggal : **22 Agustus 2026**

<br>

<table style="width:100%; text-align:center; border:none;">
  <tr>
    <td style="width:50%; border:none;">
      Mengetahui,<br>
      <b>Dosen Pembimbing Lapangan (DPL)</b><br><br><br><br><br>
      ( ............................................................ )<br>
      NIP. 
    </td>
    <td style="width:50%; border:none;">
      Menerima,<br>
      <b>Kepala Desa Pucang</b><br><br><br><br><br>
      ( ............................................................ )<br>
      NIP.
    </td>
  </tr>
  <tr>
    <td colspan="2" style="text-align:center; padding-top:40px; border:none;">
      Disusun &amp; Diserahkan Oleh:<br>
      <b>Koordinator Mahasiswa Desa (KORMADES) KKN GIAT 16 UNNES</b><br><br><br><br><br>
      ( ............................................................ )<br>
      NIM.
    </td>
  </tr>
</table>
