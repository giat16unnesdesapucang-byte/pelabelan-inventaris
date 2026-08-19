import { createClient } from '@supabase/supabase-js';

// TODO: Replace these with your actual Supabase URL and Anon Key
const supabaseUrl = import.meta.env.VITE_SUPABASE_URL || 'https://placeholder.supabase.co';
const supabaseAnonKey = import.meta.env.VITE_SUPABASE_ANON_KEY || 'placeholder-anon-key';

export const supabase = createClient(supabaseUrl, supabaseAnonKey);

export type Asset = {
  id: string;
  asset_code: string;
  name: string;
  description: string;
  condition: 'Baik' | 'Rusak Ringan' | 'Rusak Berat';
  availability_status: 'Tersedia' | 'Dipinjam' | 'Perawatan';
  location: string;
  photo_url?: string | null;
};

export type LoanTransaction = {
  id: string;
  asset_id: string;
  borrower_name: string;
  borrower_nik?: string;
  borrower_phone?: string;
  borrow_date: string;
  expected_return_date?: string;
  actual_return_date?: string;
  condition_out?: string;
  condition_in?: string;
  status: 'Aktif' | 'Selesai' | 'Terlambat';
};
