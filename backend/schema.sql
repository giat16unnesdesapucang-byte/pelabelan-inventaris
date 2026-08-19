-- Supabase Schema for Sistem Informasi Inventaris Aset Desa

-- Enable UUID extension if not already enabled
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Table: categories
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc', now()),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc', now())
);

-- Table: assets
CREATE TABLE assets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_code VARCHAR(100) UNIQUE NOT NULL,
    category_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    purchase_date DATE,
    price DECIMAL(15, 2),
    funding_source VARCHAR(255),
    location VARCHAR(255),
    condition VARCHAR(50) CHECK (condition IN ('Baik', 'Rusak Ringan', 'Rusak Berat')) DEFAULT 'Baik',
    availability_status VARCHAR(50) CHECK (availability_status IN ('Tersedia', 'Dipinjam', 'Perawatan')) DEFAULT 'Tersedia',
    photo_url TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc', now()),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc', now())
);

-- Table: loan_transactions
CREATE TABLE loan_transactions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID REFERENCES assets(id) ON DELETE CASCADE NOT NULL,
    borrower_name VARCHAR(255) NOT NULL,
    borrower_nik VARCHAR(16),
    borrower_phone VARCHAR(20),
    borrow_date TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc', now()),
    expected_return_date TIMESTAMP WITH TIME ZONE,
    actual_return_date TIMESTAMP WITH TIME ZONE,
    condition_out VARCHAR(50),
    condition_in VARCHAR(50),
    status VARCHAR(50) CHECK (status IN ('Aktif', 'Selesai', 'Terlambat')) DEFAULT 'Aktif',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc', now()),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc', now())
);

-- Triggers for updated_at

CREATE OR REPLACE FUNCTION update_modified_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_categories_modtime
    BEFORE UPDATE ON categories
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_column();

CREATE TRIGGER update_assets_modtime
    BEFORE UPDATE ON assets
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_column();

CREATE TRIGGER update_loan_transactions_modtime
    BEFORE UPDATE ON loan_transactions
    FOR EACH ROW
    EXECUTE FUNCTION update_modified_column();

-- ===================================================
-- Supabase Storage Setup: Bucket 'asset-photos'
-- ===================================================
-- Pastikan bucket 'asset-photos' dibuat dengan akses publik:
INSERT INTO storage.buckets (id, name, public)
VALUES ('asset-photos', 'asset-photos', true)
ON CONFLICT (id) DO NOTHING;

-- Policy agar semua orang bisa melihat (SELECT) foto:
CREATE POLICY "Public Access" ON storage.objects
FOR SELECT USING (bucket_id = 'asset-photos');

-- Policy agar aplikasi dapat mengunggah (INSERT) foto:
CREATE POLICY "Public Upload" ON storage.objects
FOR INSERT WITH CHECK (bucket_id = 'asset-photos');

-- Policy agar aplikasi dapat memperbarui (UPDATE) foto:
CREATE POLICY "Public Update" ON storage.objects
FOR UPDATE USING (bucket_id = 'asset-photos');

