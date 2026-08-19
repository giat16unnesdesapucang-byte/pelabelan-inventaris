import React, { useEffect, useState, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, CheckCircle2, AlertTriangle, Image as ImageIcon, User, Phone, CreditCard, Calendar, Search, ArrowDownUp, ChevronLeft, ChevronRight } from 'lucide-react';
import { supabase, type Asset } from '../lib/supabaseClient';

export interface LoanHistory {
  id: string;
  borrower_name: string;
  borrow_date: string;
  actual_return_date: string | null;
  status: string;
  condition_out?: string;
  condition_in?: string;
}

const AssetDetails = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [asset, setAsset] = useState<Asset | null>(null);
  const [history, setHistory] = useState<LoanHistory[]>([]);
  const [fullHistory, setFullHistory] = useState<LoanHistory[]>([]);
  const [showFullHistory, setShowFullHistory] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Full History Modal States
  const [historySearchQuery, setHistorySearchQuery] = useState('');
  const [historyFilterStatus, setHistoryFilterStatus] = useState('Semua');
  const [historySortOrder, setHistorySortOrder] = useState<'desc'|'asc'>('desc');
  const [historyCurrentPage, setHistoryCurrentPage] = useState(1);

  // Form states
  const [isBorrowing, setIsBorrowing] = useState(false);
  const [isReturning, setIsReturning] = useState(false);
  const [borrowerName, setBorrowerName] = useState('');
  const [borrowerNik, setBorrowerNik] = useState('');
  const [borrowerPhone, setBorrowerPhone] = useState('');
  const [expectedReturnDate, setExpectedReturnDate] = useState('');
  const [returnCondition, setReturnCondition] = useState('Baik');

  const filteredAndSortedHistory = useMemo(() => {
    let result = [...fullHistory];
    if (historyFilterStatus !== 'Semua') {
      result = result.filter(item => item.status === historyFilterStatus);
    }
    if (historySearchQuery) {
      result = result.filter(item => 
        (item.borrower_name || '').toLowerCase().includes(historySearchQuery.toLowerCase())
      );
    }
    result.sort((a, b) => {
      const dateA = new Date(a.borrow_date).getTime();
      const dateB = new Date(b.borrow_date).getTime();
      return historySortOrder === 'asc' ? dateA - dateB : dateB - dateA;
    });
    return result;
  }, [fullHistory, historySearchQuery, historyFilterStatus, historySortOrder]);

  const itemsPerPage = 10;
  const totalHistoryPages = Math.ceil(filteredAndSortedHistory.length / itemsPerPage);
  const paginatedHistory = filteredAndSortedHistory.slice(
    (historyCurrentPage - 1) * itemsPerPage,
    historyCurrentPage * itemsPerPage
  );

  useEffect(() => {
    setHistoryCurrentPage(1);
  }, [historySearchQuery, historyFilterStatus, historySortOrder]);

  useEffect(() => {
    const fetchAsset = async () => {
      try {
        setLoading(true);
        const { data, error } = await supabase
          .from('assets')
          .select('*')
          .eq('asset_code', id)
          .single();

        if (error) throw error;
        if (data) {
          setAsset(data as Asset);
          setReturnCondition(data.condition || 'Baik');
          
          // Fetch history
          const { data: historyData } = await supabase
            .from('loan_transactions')
            .select('id, borrower_name, borrow_date, actual_return_date, status')
            .eq('asset_id', data.id)
            .order('borrow_date', { ascending: false })
            .limit(5);
            
          if (historyData) setHistory(historyData);
        }
      } catch (err: any) {
        setError(err.message || 'Gagal mengambil data aset.');
      } finally {
        setLoading(false);
      }
    };

    if (id) fetchAsset();
  }, [id]);

  if (loading) {
    return (
      <div className="page-container" style={{ justifyContent: 'center', alignItems: 'center' }}>
        <div style={{
          width: '40px',
          height: '40px',
          border: '3px solid var(--surface-border)',
          borderTopColor: 'var(--accent-color)',
          borderRadius: '50%',
          animation: 'spin 1s linear infinite'
        }} />
        <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
      </div>
    );
  }

  if (error || !asset) {
    return (
      <div className="page-container" style={{ justifyContent: 'center', alignItems: 'center', textAlign: 'center' }}>
        <AlertTriangle size={48} color="var(--status-bad)" style={{ marginBottom: '16px' }} />
        <h2>Data Tidak Ditemukan</h2>
        <p style={{ color: 'var(--text-secondary)', marginTop: '8px' }}>{error}</p>
        <button 
          onClick={() => navigate(-1)}
          style={{ marginTop: '24px', padding: '12px 24px', background: 'var(--surface-color)', border: '1px solid var(--surface-border)', borderRadius: 'var(--radius-md)' }}>
          Kembali
        </button>
      </div>
    );
  }

  const getDisplayStatus = (status: string) => {
    if (status === 'Aktif') return 'Dipinjam';
    if (status === 'Selesai') return 'Dikembalikan';
    return status;
  };

  const isAvailable = asset.availability_status === 'Tersedia';

  const loadFullHistory = async () => {
    if (fullHistory.length > 0) {
      setShowFullHistory(true);
      return;
    }
    try {
      const { data, error } = await supabase
        .from('loan_transactions')
        .select('id, borrower_name, borrow_date, actual_return_date, status, condition_out, condition_in')
        .eq('asset_id', asset?.id)
        .order('borrow_date', { ascending: false });
      if (error) throw error;
      setFullHistory(data || []);
      setShowFullHistory(true);
    } catch (err: any) {
      alert('Gagal memuat riwayat lengkap');
    }
  };

  const updateCondition = async (newCondition: string) => {
    try {
      setLoading(true);
      const { error } = await supabase
        .from('assets')
        .update({ condition: newCondition })
        .eq('id', asset.id);
        
      if (error) throw error;
      setAsset({ ...asset, condition: newCondition as any });
    } catch (err: any) {
      alert('Gagal mengubah kondisi: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const submitBorrow = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!asset || !borrowerName) return;
    
    try {
      setLoading(true);
      // 1. Insert ke loan_transactions
      const { error: loanError } = await supabase
        .from('loan_transactions')
        .insert({
          asset_id: asset.id,
          borrower_name: borrowerName,
          borrower_nik: borrowerNik || null,
          borrower_phone: borrowerPhone || null,
          expected_return_date: expectedReturnDate || null,
          condition_out: asset.condition,
          status: 'Aktif'
        });

      if (loanError) throw loanError;

      // 2. Update status aset
      const { error: assetError } = await supabase
        .from('assets')
        .update({ availability_status: 'Dipinjam' })
        .eq('id', asset.id);

      if (assetError) throw assetError;
      
      setAsset({ ...asset, availability_status: 'Dipinjam' });
      setIsBorrowing(false);
      
      // Reset form
      setBorrowerName('');
      setBorrowerNik('');
      setBorrowerPhone('');
      setExpectedReturnDate('');
      
    } catch (err: any) {
      alert('Gagal memproses peminjaman: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const submitReturn = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!asset) return;
    
    try {
      setLoading(true);
      
      // 1. Cari transaksi peminjaman aktif untuk aset ini
      const { data: activeLoan, error: fetchError } = await supabase
        .from('loan_transactions')
        .select('id')
        .eq('asset_id', asset.id)
        .eq('status', 'Aktif')
        .single();
        
      if (fetchError && fetchError.code !== 'PGRST116') {
        throw fetchError;
      }
      
      // 2. Jika ditemukan transaksi, update statusnya menjadi selesai
      if (activeLoan) {
        const { error: updateLoanError } = await supabase
          .from('loan_transactions')
          .update({
            status: 'Selesai',
            actual_return_date: new Date().toISOString(),
            condition_in: returnCondition
          })
          .eq('id', activeLoan.id);
          
        if (updateLoanError) throw updateLoanError;
      }
      
      // 3. Update ketersediaan aset menjadi Tersedia
      const { error: assetError } = await supabase
        .from('assets')
        .update({ 
          availability_status: 'Tersedia',
          condition: returnCondition
        })
        .eq('id', asset.id);

      if (assetError) throw assetError;
      
      setAsset({ ...asset, availability_status: 'Tersedia', condition: returnCondition as any });
      setIsReturning(false);
      
    } catch (err: any) {
      alert('Gagal memproses pengembalian: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  // Helper styles for form inputs
  const inputContainerStyle = {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    background: 'rgba(255, 255, 255, 0.5)',
    border: '1px solid var(--surface-border)',
    borderRadius: 'var(--radius-sm)',
    padding: '12px 16px',
    marginBottom: '12px'
  };

  const inputStyle = {
    flex: 1,
    border: 'none',
    background: 'transparent',
    outline: 'none',
    fontSize: '14px',
    color: 'var(--text-primary)',
    width: '100%'
  };

  return (
    <>
      <div className="page-container animate-slide-up" style={{ paddingBottom: '180px' }}>
      <header style={{ display: 'flex', alignItems: 'center', gap: '16px', marginBottom: 'var(--spacing-md)' }}>
        <button 
          onClick={() => {
            if (isBorrowing) setIsBorrowing(false);
            else if (isReturning) setIsReturning(false);
            else navigate(-1);
          }} 
          style={{ width: '40px', height: '40px', borderRadius: '50%', background: 'var(--surface-color)', display: 'flex', alignItems: 'center', justifyContent: 'center', border: '1px solid var(--surface-border)' }}
        >
          <ArrowLeft size={20} />
        </button>
        <h1 style={{ fontSize: '20px' }}>
          {isBorrowing ? 'Form Peminjaman' : isReturning ? 'Pengembalian Aset' : 'Detail Aset'}
        </h1>
      </header>

      {/* Tampilan Detail Normal */}
      <div style={{ display: (isBorrowing || isReturning) ? 'none' : 'block' }}>
        <div style={{ 
          width: '100%', height: '200px', borderRadius: 'var(--radius-md)', 
          background: 'var(--surface-color)', border: '1px solid var(--surface-border)',
          display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-secondary)',
          marginBottom: 'var(--spacing-lg)', overflow: 'hidden'
        }}>
          {asset.photo_url ? (
            <img src={asset.photo_url} alt={asset.name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
          ) : (
            <div style={{ textAlign: 'center' }}>
              <ImageIcon size={48} strokeWidth={1} style={{ opacity: 0.5, margin: '0 auto 8px' }} />
              <p style={{ fontSize: '14px' }}>Foto tidak tersedia</p>
            </div>
          )}
        </div>

        <div className="glass-panel" style={{ padding: 'var(--spacing-lg)', marginBottom: 'var(--spacing-lg)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '16px' }}>
            <div>
              <p style={{ fontSize: '12px', color: 'var(--text-secondary)', fontWeight: 600, letterSpacing: '0.05em' }}>
                {asset.asset_code}
              </p>
              <h2 style={{ fontSize: '24px', marginTop: '4px' }}>{asset.name}</h2>
            </div>
            
            {isAvailable ? (
              <div style={{ 
                padding: '6px 12px', borderRadius: 'var(--radius-full)', 
                background: 'var(--accent-light)',
                color: 'var(--accent-hover)',
                fontSize: '12px', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '6px'
              }}>
                <CheckCircle2 size={14} />
                {asset.availability_status}
              </div>
            ) : (
              <div>
                <div style={{ 
                  padding: '6px 12px', borderRadius: 'var(--radius-full)', 
                  background: 'rgba(239, 68, 68, 0.1)',
                  color: 'var(--status-bad)',
                  fontSize: '12px', fontWeight: 600
                }}>
                  Aset Sedang Dipinjam
                </div>
                <p style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '8px' }}>
                  Kembalikan aset ini agar dapat dipinjam oleh orang lain.
                </p>
              </div>
            )}
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
            <div>
              <p style={{ fontSize: '11px', color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '4px' }}>Lokasi</p>
              <p style={{ fontWeight: 500 }}>{asset.location}</p>
            </div>
            <div>
              <p style={{ fontSize: '11px', color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '4px' }}>Kondisi</p>
              <select 
                value={asset.condition}
                onChange={(e) => updateCondition(e.target.value)}
                style={{ 
                  fontWeight: 500, background: 'rgba(255, 255, 255, 0.5)', border: '1px solid var(--surface-border)', 
                  borderRadius: 'var(--radius-sm)', padding: '6px 8px', color: 'var(--text-primary)',
                  outline: 'none', fontSize: '14px', width: '100%', cursor: 'pointer'
                }}
              >
                <option value="Baik">Baik</option>
                <option value="Rusak Ringan">Rusak Ringan</option>
                <option value="Rusak Berat">Rusak Berat</option>
              </select>
            </div>
          </div>
        </div>

        {/* Riwayat Peminjaman Terakhir */}
        <div style={{ marginBottom: 'var(--spacing-lg)' }}>
          <h3 style={{ fontSize: '16px', marginBottom: '12px', fontWeight: 600 }}>Riwayat Peminjaman Terakhir</h3>
          {history.length === 0 ? (
            <div className="glass-panel" style={{ padding: '16px', textAlign: 'center', color: 'var(--text-secondary)' }}>
              <p style={{ fontSize: '14px' }}>Belum ada riwayat peminjaman.</p>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {history.map(item => (
                <div key={item.id} className="glass-panel" style={{ padding: '12px 16px', borderLeft: `4px solid ${item.status === 'Selesai' ? 'var(--status-good)' : 'var(--status-bad)'}` }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' }}>
                    <p style={{ fontWeight: 600, fontSize: '14px' }}>{item.borrower_name}</p>
                    <span style={{ fontSize: '11px', padding: '2px 8px', borderRadius: '4px', background: item.status === 'Selesai' ? 'rgba(16, 185, 129, 0.1)' : 'rgba(239, 68, 68, 0.1)', color: item.status === 'Selesai' ? 'var(--status-good)' : 'var(--status-bad)' }}>
                      {getDisplayStatus(item.status)}
                    </span>
                  </div>
                  <p style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                    {new Date(item.borrow_date).toLocaleDateString('id-ID', { day: 'numeric', month: 'short', year: 'numeric' })}
                    {item.actual_return_date ? ` - ${new Date(item.actual_return_date).toLocaleDateString('id-ID', { day: 'numeric', month: 'short', year: 'numeric' })}` : ' - Sekarang'}
                  </p>
                </div>
              ))}
            </div>
          )}
          {history.length >= 1 && (
            <button onClick={loadFullHistory} style={{ width: '100%', padding: '12px', marginTop: '12px', background: 'var(--accent-light)', border: '1px solid var(--accent-color)', borderRadius: 'var(--radius-md)', color: 'var(--accent-hover)', fontWeight: 600 }}>
              Lihat Semua Riwayat
            </button>
          )}
        </div>
      </div>

      {/* Tampilan Form Pengembalian */}
      <div style={{ display: isReturning ? 'block' : 'none' }}>
        <div className="glass-panel" style={{ padding: 'var(--spacing-lg)' }}>
          <form onSubmit={submitReturn}>
            
            <div style={{ marginBottom: '16px' }}>
              <label style={{ display: 'block', fontSize: '14px', marginBottom: '8px', fontWeight: 500 }}>
                Kondisi Aset Saat Kembali
              </label>
              <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                {['Baik', 'Rusak Ringan', 'Rusak Berat'].map(cond => (
                  <button
                    key={cond}
                    type="button"
                    onClick={() => setReturnCondition(cond)}
                    style={{
                      flex: '1', minWidth: '30%',
                      padding: '10px 8px', borderRadius: 'var(--radius-md)',
                      fontSize: '14px', fontWeight: 500, border: '1px solid',
                      background: returnCondition === cond ? 'rgba(59, 130, 246, 0.1)' : 'var(--surface-color)',
                      borderColor: returnCondition === cond ? 'var(--accent-color)' : 'var(--surface-border)',
                      color: returnCondition === cond ? 'var(--accent-color)' : 'var(--text-primary)'
                    }}
                  >
                    {cond}
                  </button>
                ))}
              </div>
              <p style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '8px' }}>
                *Pastikan mengecek kondisi fisik secara menyeluruh sebelum menyetujui pengembalian.
              </p>
            </div>

            <button 
              type="submit"
              disabled={loading}
              style={{ 
                width: '100%', padding: '16px', marginTop: '24px',
                background: 'var(--accent-color)', color: 'white', borderRadius: 'var(--radius-md)',
                fontWeight: 600, fontSize: '16px', border: 'none',
                opacity: loading ? 0.7 : 1, cursor: loading ? 'wait' : 'pointer'
              }}>
              {loading ? 'Menyimpan...' : 'Selesaikan Transaksi & Kembalikan'}
            </button>
          </form>
        </div>
      </div>

      {/* Form Peminjaman (Glassmorphism) */}
      {isBorrowing && (
        <form onSubmit={submitBorrow} className="glass-panel animate-slide-up" style={{ padding: 'var(--spacing-lg)' }}>
          <h3 style={{ fontSize: '16px', marginBottom: '16px' }}>Data Peminjam</h3>
          
          <div style={inputContainerStyle}>
            <User size={18} color="var(--text-secondary)" />
            <input 
              type="text" 
              placeholder="Nama Peminjam (Wajib)" 
              required
              value={borrowerName}
              onChange={(e) => setBorrowerName(e.target.value)}
              style={inputStyle} 
            />
          </div>

          <div style={inputContainerStyle}>
            <CreditCard size={18} color="var(--text-secondary)" />
            <input 
              type="text" 
              placeholder="NIK / KTP (Opsional)" 
              value={borrowerNik}
              onChange={(e) => setBorrowerNik(e.target.value)}
              style={inputStyle} 
            />
          </div>

          <div style={inputContainerStyle}>
            <Phone size={18} color="var(--text-secondary)" />
            <input 
              type="tel" 
              placeholder="No. Telepon (Opsional)" 
              value={borrowerPhone}
              onChange={(e) => setBorrowerPhone(e.target.value)}
              style={inputStyle} 
            />
          </div>

          <h3 style={{ fontSize: '16px', marginTop: '20px', marginBottom: '16px' }}>Rencana Kembali</h3>
          
          <div style={inputContainerStyle}>
            <Calendar size={18} color="var(--text-secondary)" />
            <input 
              type="date" 
              value={expectedReturnDate}
              onChange={(e) => setExpectedReturnDate(e.target.value)}
              style={inputStyle} 
            />
          </div>

          <div style={{ display: 'flex', gap: '12px', marginTop: '24px' }}>
            <button 
              type="button"
              onClick={() => setIsBorrowing(false)}
              style={{
                flex: 1, padding: '14px', background: 'transparent', color: 'var(--text-primary)',
                borderRadius: 'var(--radius-md)', fontWeight: 600, border: '1px solid var(--surface-border)'
              }}
            >
              Batal
            </button>
            <button 
              type="submit"
              style={{
                flex: 2, padding: '14px', background: 'var(--accent-color)', color: 'white',
                borderRadius: 'var(--radius-md)', fontWeight: 600, border: 'none',
                boxShadow: '0 4px 12px rgba(16, 185, 129, 0.2)'
              }}
            >
              Simpan Pinjaman
            </button>
          </div>
        </form>
      )}
    </div>

    {/* Action Buttons (Fixed Outside Transform) */}
    {!(isBorrowing || isReturning) && (
      <div style={{ 
        display: 'flex', 
        gap: '12px',
        position: 'fixed',
        bottom: '80px',
        left: 0,
        right: 0,
        padding: '16px',
        background: 'var(--bg-color)',
        borderTop: '1px solid var(--surface-border)',
        zIndex: 40,
        maxWidth: '480px',
        margin: '0 auto'
      }}>
        {isAvailable ? (
          <button 
            onClick={() => setIsBorrowing(true)}
            style={{
            flex: 1, padding: '16px', background: 'var(--accent-color)', color: 'white',
            borderRadius: 'var(--radius-md)', fontWeight: 600, fontSize: '16px',
            boxShadow: '0 4px 12px rgba(16, 185, 129, 0.2)'
          }}>
            Pinjamkan Aset
          </button>
        ) : (
          <button 
            onClick={() => setIsReturning(true)}
            style={{
            flex: 1, padding: '16px', background: '#3B82F6', color: 'white',
            borderRadius: 'var(--radius-md)', fontWeight: 600, fontSize: '16px',
            boxShadow: '0 4px 12px rgba(59, 130, 246, 0.2)'
          }}>
            Kembalikan Aset
          </button>
        )}
      </div>
    )}

    {/* Full History Modal */}
    {showFullHistory && (
      <div className="animate-slide-up" style={{
        position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
        background: 'var(--bg-color)', zIndex: 100, overflowY: 'auto',
        padding: 'var(--spacing-md)'
      }}>
        <header style={{ display: 'flex', alignItems: 'center', gap: '16px', marginBottom: 'var(--spacing-md)' }}>
          <button 
            onClick={() => setShowFullHistory(false)} 
            style={{ width: '40px', height: '40px', borderRadius: '50%', background: 'var(--surface-color)', display: 'flex', alignItems: 'center', justifyContent: 'center', border: '1px solid var(--surface-border)' }}
          >
            <ArrowLeft size={20} />
          </button>
          <h1 style={{ fontSize: '20px' }}>Riwayat Lengkap</h1>
        </header>

        {/* Filters & Search */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginBottom: '16px' }}>
          <div style={{ display: 'flex', gap: '8px', background: 'var(--surface-color)', padding: '12px', borderRadius: 'var(--radius-md)', border: '1px solid var(--surface-border)' }}>
            <Search size={20} color="var(--text-secondary)" />
            <input 
              type="text" 
              placeholder="Cari nama peminjam..." 
              value={historySearchQuery}
              onChange={(e) => setHistorySearchQuery(e.target.value)}
              style={{ border: 'none', background: 'transparent', outline: 'none', width: '100%', color: 'var(--text-primary)' }}
            />
          </div>
          <div style={{ display: 'flex', gap: '8px' }}>
            <select 
              value={historyFilterStatus}
              onChange={(e) => setHistoryFilterStatus(e.target.value)}
              style={{ flex: 1, padding: '10px', borderRadius: 'var(--radius-md)', background: 'var(--surface-color)', border: '1px solid var(--surface-border)', color: 'var(--text-primary)' }}
            >
              <option value="Semua">Semua Status</option>
              <option value="Aktif">Dipinjam</option>
              <option value="Selesai">Dikembalikan</option>
              <option value="Terlambat">Terlambat</option>
            </select>
            <button 
              onClick={() => setHistorySortOrder(prev => prev === 'desc' ? 'asc' : 'desc')}
              style={{ display: 'flex', alignItems: 'center', gap: '6px', padding: '10px 16px', borderRadius: 'var(--radius-md)', background: 'var(--surface-color)', border: '1px solid var(--surface-border)' }}
            >
              <ArrowDownUp size={16} />
              <span style={{ fontSize: '13px', fontWeight: 500 }}>{historySortOrder === 'desc' ? 'Terbaru' : 'Terlama'}</span>
            </button>
          </div>
        </div>
        
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          {paginatedHistory.length === 0 && (
            <div style={{ textAlign: 'center', padding: '40px 20px', color: 'var(--text-secondary)' }}>
              Data tidak ditemukan
            </div>
          )}
          {paginatedHistory.map(item => (
              <div key={item.id} className="glass-panel" style={{ padding: '16px', borderLeft: `4px solid ${item.status === 'Selesai' ? 'var(--status-good)' : 'var(--status-bad)'}` }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
                  <p style={{ fontWeight: 600, fontSize: '16px' }}>{item.borrower_name}</p>
                  <span style={{ fontSize: '12px', padding: '4px 8px', borderRadius: '4px', background: item.status === 'Selesai' ? 'rgba(16, 185, 129, 0.1)' : 'rgba(239, 68, 68, 0.1)', color: item.status === 'Selesai' ? 'var(--status-good)' : 'var(--status-bad)', fontWeight: 600 }}>
                    {getDisplayStatus(item.status)}
                  </span>
                </div>
                <div style={{ fontSize: '12px', color: 'var(--text-secondary)', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                  <div>
                    <p style={{ fontSize: '10px', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '2px' }}>Tgl Pinjam</p>
                    <p style={{ color: 'var(--text-primary)', fontWeight: 500 }}>{new Date(item.borrow_date).toLocaleDateString('id-ID', { day: 'numeric', month: 'short', year: 'numeric' })}</p>
                  </div>
                  <div>
                    <p style={{ fontSize: '10px', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '2px' }}>Tgl Kembali</p>
                    <p style={{ color: 'var(--text-primary)', fontWeight: 500 }}>{item.actual_return_date ? new Date(item.actual_return_date).toLocaleDateString('id-ID', { day: 'numeric', month: 'short', year: 'numeric' }) : '-'}</p>
                  </div>
                  <div>
                    <p style={{ fontSize: '10px', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '2px' }}>Kond. Keluar</p>
                    <p style={{ color: 'var(--text-primary)' }}>{item.condition_out || '-'}</p>
                  </div>
                  <div>
                    <p style={{ fontSize: '10px', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '2px' }}>Kond. Masuk</p>
                    <p style={{ color: 'var(--text-primary)' }}>{item.condition_in || '-'}</p>
                  </div>
                </div>
              </div>
          ))}
        </div>

        {/* Pagination Controls */}
        {totalHistoryPages > 1 && (
          <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '12px', marginTop: '24px', paddingBottom: '40px' }}>
            <button 
              disabled={historyCurrentPage === 1} 
              onClick={() => setHistoryCurrentPage(p => Math.max(1, p - 1))} 
              style={{ padding: '8px', borderRadius: '8px', background: 'var(--surface-color)', border: '1px solid var(--surface-border)', opacity: historyCurrentPage === 1 ? 0.5 : 1 }}
            >
              <ChevronLeft size={20} />
            </button>
            <span style={{ fontSize: '14px', fontWeight: 600 }}>
              Halaman {historyCurrentPage} dari {totalHistoryPages}
            </span>
            <button 
              disabled={historyCurrentPage === totalHistoryPages} 
              onClick={() => setHistoryCurrentPage(p => Math.min(totalHistoryPages, p + 1))} 
              style={{ padding: '8px', borderRadius: '8px', background: 'var(--surface-color)', border: '1px solid var(--surface-border)', opacity: historyCurrentPage === totalHistoryPages ? 0.5 : 1 }}
            >
              <ChevronRight size={20} />
            </button>
          </div>
        )}
        {totalHistoryPages <= 1 && <div style={{ paddingBottom: '40px' }} />}
      </div>
    )}
    </>
  );
};

export default AssetDetails;
