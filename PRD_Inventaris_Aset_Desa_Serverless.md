# Product Requirements Document (PRD)
## Sistem Informasi Inventaris & Peminjaman Aset Desa (Serverless Architecture)

### 1. Ringkasan Eksekutif
Sistem Informasi Inventaris Aset Desa adalah platform digital untuk mencatat, melacak fisik, dan mengelola sirkulasi barang milik balai desa. Sistem ini dirancang menggunakan arsitektur tanpa server (Serverless/BaaS) dan aplikasi mandiri untuk memastikan tidak ada beban pemeliharaan infrastruktur (VPS/localhost) bagi perangkat desa.

### 2. Tujuan & Sasaran
*   **Nihil Pemeliharaan Server:** Menggunakan Backend-as-a-Service (BaaS) agar database berjalan otomatis di cloud.
*   **Digitalisasi Data:** Beralih dari pencatatan manual ke database cloud relasional.
*   **Pelacakan Cepat:** Memanfaatkan QR Code fisik yang terintegrasi dengan pemindai mobile.
*   **Akuntabilitas Sirkulasi:** Mencatat siapa yang meminjam aset dan kondisi barang secara presisi.

### 3. Peran Pengguna (User Roles)
1.  **Admin / Pengelola Aset (Desktop User):** Perangkat desa yang menggunakan komputer balai desa untuk manajemen data skala besar, cetak label, dan rekapitulasi.
2.  **Petugas Lapangan / Scanner (Mobile User):** Perangkat desa yang mengecek kondisi barang atau memproses peminjaman/pengembalian langsung di lokasi barang.

### 4. Spesifikasi Fungsional
#### 4.1. Manajemen Aset Inti (Aplikasi Desktop)
*   Operasi CRUD penuh pada data aset.
*   Setiap aset diberi ID unik (SKU) secara otomatis.
*   Penyimpanan detail aset: Nama, Kategori, Tanggal Beli, Harga, Sumber Dana, Lokasi, Kondisi, dan Foto.

#### 4.2. QR Code Generator & Cetak Label (Aplikasi Desktop)
*   Sistem menghasilkan QR Code yang berisi payload ID Aset unik (misal: `INV-2026-001`).
*   **Batch Print:** Admin dapat memilih puluhan barang sekaligus untuk dicetak label QR-nya langsung ke printer balai desa dalam format stiker.

#### 4.3. Pemindaian & Pembaruan Status (Aplikasi Mobile)
*   Pemindaian QR code menggunakan kamera smartphone.
*   Saat dipindai, aplikasi mengambil data langsung dari database cloud dan menampilkan status terkini barang.
*   Terdapat tombol aksi cepat: "Ubah Kondisi", "Pinjamkan", atau "Kembalikan" langsung dari antarmuka pemindai.

#### 4.4. Manajemen Sirkulasi
*   Pencatatan peminjaman dengan detail: Nama Peminjam, NIK, Tanggal Kembali, dan Kondisi.
*   Status ketersediaan aset ter-update seketika (Real-time) di cloud.
*   Pencatatan riwayat (log) permanen untuk setiap transaksi peminjaman.

### 5. Spesifikasi Teknis (Tech Stack)
*   **Arsitektur:** Serverless Backend dengan Decoupled Front-End.
*   **Backend, Auth & Database:** Supabase (PostgreSQL) atau Firebase. Tanpa perlu konfigurasi server manual.
*   **Aplikasi Admin (Desktop):** C# (.NET). Optimal untuk pengolahan data intensif, koneksi periferal (printer lokal), dan performa tinggi di komputer Windows balai desa.
*   **Aplikasi Scanner (Mobile):** Progressive Web App (PWA). Solusi lintas platform yang ringan, bisa di-install langsung ke *homescreen* smartphone tanpa harus publish ke App Store/Play Store, dan mudah di-update.

### 6. Skema Database (Cloud)

**Table: categories**
*   id (UUID, PK), name, description

**Table: assets**
*   id (UUID, PK), asset_code (String, Unique)
*   category_id (UUID, FK)
*   name, description
*   purchase_date, price
*   funding_source
*   location
*   condition (Enum: Baik, Rusak Ringan, Rusak Berat)
*   availability_status (Enum: Tersedia, Dipinjam, Perawatan)
*   photo_url (String)

**Table: loan_transactions**
*   id (UUID, PK)
*   asset_id (UUID, FK)
*   borrower_name, borrower_nik, borrower_phone
*   borrow_date, expected_return_date, actual_return_date
*   condition_out, condition_in
*   status (Enum: Aktif, Selesai, Terlambat)

### 7. Alur Pengalaman Pengguna (User Flow)
#### Alur Desktop (C# .NET)
1. Admin membuka aplikasi di PC Windows balai desa dan login.
2. Admin masuk ke menu "Data Aset" untuk menginput meja dan kursi baru.
3. Admin masuk ke menu "Cetak Label", menyeleksi aset yang baru diinput, dan mengeklik "Print Batch".
4. Stiker keluar dari printer dan siap ditempel.

#### Alur Mobile Scanner (PWA)
1. Petugas desa membuka aplikasi PWA dari *homescreen* HP.
2. Mengeklik ikon Kamera/Scan.
3. Mengarahkan kamera ke stiker QR Code di kursi balai desa.
4. Aplikasi membaca ID aset, melakukan *fetch* ke Supabase/Firebase, dan menampilkan profil kursi.
5. Petugas mengklik "Pinjamkan" dan menyerahkan kursi kepada warga.

### 8. Kriteria Penerimaan (Acceptance Criteria)
*   Aplikasi desktop berjalan stabil di OS Windows dan dapat terhubung langsung dengan layanan cloud.
*   Aplikasi mobile (PWA) dapat mengakses kamera perangkat Android/iOS dengan lancar.
*   Sinkronisasi data (seperti perubahan status aset) ter-update secara *real-time* di seluruh perangkat.
*   Sistem tidak memerlukan *restart*, pemeliharaan server, atau penanganan VPS secara manual oleh pihak balai desa.
