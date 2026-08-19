import { useEffect, useState, useMemo } from 'react';
import { supabase } from '../lib/supabaseClient';
import { Search, ArrowDownUp, ChevronLeft, ChevronRight, AlertCircle, ArrowUpRight, ArrowDownRight } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

interface HistoryItem {
  id: string;
  asset_code: string;
  asset_name: string;
  borrower_name: string;
  borrow_date: string;
  actual_return_date: string | null;
  expected_return_date: string;
  status: string;
}

const History = () => {
  const navigate = useNavigate();
  const [history, setHistory] = useState<HistoryItem[]>([]);
  const [loading, setLoading] = useState(true);

  // States
  const [searchQuery, setSearchQuery] = useState('');
  const [filterStatus, setFilterStatus] = useState('Semua');
  const [sortOrder, setSortOrder] = useState<'desc'|'asc'>('desc');
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  useEffect(() => {
    const fetchHistory = async () => {
      setLoading(true);
      try {
        const { data, error } = await supabase
          .from('loan_transactions')
          .select('id, borrower_name, borrow_date, actual_return_date, expected_return_date, status, assets(asset_code, name)')
          .order('borrow_date', { ascending: false });

        if (error) throw error;

        if (data) {
          const mapped = data.map((item: any) => ({
            id: item.id,
            asset_code: item.assets?.asset_code,
            asset_name: item.assets?.name,
            borrower_name: item.borrower_name,
            borrow_date: item.borrow_date,
            actual_return_date: item.actual_return_date,
            expected_return_date: item.expected_return_date,
            status: item.status
          }));
          setHistory(mapped);
        }
      } catch (err) {
        console.error('Error fetching history:', err);
      } finally {
        setLoading(false);
      }
    };
    fetchHistory();
  }, []);

  const getDisplayStatus = (status: string) => {
    if (status === 'Aktif') return 'Dipinjam';
    if (status === 'Selesai') return 'Dikembalikan';
    return status;
  };

  const filteredAndSortedHistory = useMemo(() => {
    let result = [...history];
    if (filterStatus !== 'Semua') {
      result = result.filter(item => item.status === filterStatus);
    }
    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      result = result.filter(item => 
        (item.borrower_name || '').toLowerCase().includes(q) ||
        (item.asset_name || '').toLowerCase().includes(q) ||
        (item.asset_code || '').toLowerCase().includes(q)
      );
    }
    result.sort((a, b) => {
      const dateA = new Date(a.borrow_date).getTime();
      const dateB = new Date(b.borrow_date).getTime();
      return sortOrder === 'asc' ? dateA - dateB : dateB - dateA;
    });
    return result;
  }, [history, searchQuery, filterStatus, sortOrder]);

  const totalPages = Math.ceil(filteredAndSortedHistory.length / itemsPerPage);
  const paginatedHistory = filteredAndSortedHistory.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage
  );

  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, filterStatus, sortOrder]);

  return (
    <div className="page-container animate-slide-up" style={{ paddingBottom: '120px' }}>
      <header style={{ marginBottom: 'var(--spacing-md)' }}>
        <h1 style={{ fontSize: '28px', lineHeight: 1.2 }}>Riwayat Sirkulasi</h1>
        <p style={{ color: 'var(--text-secondary)', fontSize: '14px', marginTop: '4px' }}>
          Seluruh log peminjaman dan pengembalian aset.
        </p>
      </header>

      {/* Filters & Search */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginBottom: '16px' }}>
        <div style={{ display: 'flex', gap: '8px', background: 'var(--surface-color)', padding: '12px', borderRadius: 'var(--radius-md)', border: '1px solid var(--surface-border)' }}>
          <Search size={20} color="var(--text-secondary)" />
          <input 
            type="text" 
            placeholder="Cari warga, aset, atau kode..." 
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            style={{ border: 'none', background: 'transparent', outline: 'none', width: '100%', color: 'var(--text-primary)' }}
          />
        </div>
        <div style={{ display: 'flex', gap: '8px' }}>
          <select 
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
            style={{ flex: 1, padding: '10px', borderRadius: 'var(--radius-md)', background: 'var(--surface-color)', border: '1px solid var(--surface-border)', color: 'var(--text-primary)' }}
          >
            <option value="Semua">Semua Status</option>
            <option value="Aktif">Dipinjam</option>
            <option value="Selesai">Dikembalikan</option>
            <option value="Terlambat">Terlambat</option>
          </select>
          <button 
            onClick={() => setSortOrder(prev => prev === 'desc' ? 'asc' : 'desc')}
            style={{ display: 'flex', alignItems: 'center', gap: '6px', padding: '10px 16px', borderRadius: 'var(--radius-md)', background: 'var(--surface-color)', border: '1px solid var(--surface-border)' }}
          >
            <ArrowDownUp size={16} />
            <span style={{ fontSize: '13px', fontWeight: 500 }}>{sortOrder === 'desc' ? 'Terbaru' : 'Terlama'}</span>
          </button>
        </div>
      </div>

      {loading ? (
        <div style={{ padding: '40px', display: 'flex', justifyContent: 'center' }}>
           <div style={{ width: '40px', height: '40px', border: '3px solid var(--surface-border)', borderTopColor: 'var(--accent-color)', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />
        </div>
      ) : (
        <>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            {paginatedHistory.length === 0 && (
              <div style={{ textAlign: 'center', padding: '40px 20px', color: 'var(--text-secondary)' }}>
                Data tidak ditemukan
              </div>
            )}
            {paginatedHistory.map(item => (
              <div 
                key={item.id} 
                onClick={() => navigate(`/asset/${item.asset_code}`)}
                className="glass-panel" 
                style={{ 
                  padding: '16px', 
                  borderLeft: `4px solid ${item.status === 'Selesai' ? 'var(--status-good)' : item.status === 'Terlambat' ? 'var(--status-bad)' : 'var(--accent-color)'}`,
                  cursor: 'pointer'
                }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '12px' }}>
                  <div>
                    <p style={{ fontWeight: 600, fontSize: '15px', marginBottom: '2px' }}>{item.asset_name}</p>
                    <p style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>{item.asset_code} • {item.borrower_name}</p>
                  </div>
                  <span style={{ 
                    fontSize: '11px', padding: '4px 8px', borderRadius: '4px', fontWeight: 600,
                    background: item.status === 'Selesai' ? 'rgba(16, 185, 129, 0.1)' : item.status === 'Terlambat' ? 'rgba(239, 68, 68, 0.1)' : 'rgba(59, 130, 246, 0.1)', 
                    color: item.status === 'Selesai' ? 'var(--status-good)' : item.status === 'Terlambat' ? 'var(--status-bad)' : '#3B82F6', 
                  }}>
                    {getDisplayStatus(item.status)}
                  </span>
                </div>
                
                <div style={{ display: 'flex', gap: '16px', fontSize: '12px', color: 'var(--text-secondary)' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                     <ArrowUpRight size={14} />
                     <span>{new Date(item.borrow_date).toLocaleDateString('id-ID', { day: 'numeric', month: 'short', year: 'numeric' })}</span>
                  </div>
                  {item.status === 'Selesai' ? (
                    <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <ArrowDownRight size={14} />
                      <span>{item.actual_return_date ? new Date(item.actual_return_date).toLocaleDateString('id-ID', { day: 'numeric', month: 'short', year: 'numeric' }) : '-'}</span>
                    </div>
                  ) : (
                    <div style={{ display: 'flex', alignItems: 'center', gap: '4px', color: item.status === 'Terlambat' ? 'var(--status-bad)' : 'var(--text-secondary)' }}>
                      <AlertCircle size={14} />
                      <span>Batas: {new Date(item.expected_return_date).toLocaleDateString('id-ID', { day: 'numeric', month: 'short', year: 'numeric' })}</span>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>

          {/* Pagination Controls */}
          {totalPages > 1 && (
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '12px', marginTop: '24px' }}>
              <button 
                disabled={currentPage === 1} 
                onClick={() => setCurrentPage(p => Math.max(1, p - 1))} 
                style={{ padding: '8px', borderRadius: '8px', background: 'var(--surface-color)', border: '1px solid var(--surface-border)', opacity: currentPage === 1 ? 0.5 : 1 }}
              >
                <ChevronLeft size={20} />
              </button>
              <span style={{ fontSize: '14px', fontWeight: 600 }}>
                Halaman {currentPage} dari {totalPages}
              </span>
              <button 
                disabled={currentPage === totalPages} 
                onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))} 
                style={{ padding: '8px', borderRadius: '8px', background: 'var(--surface-color)', border: '1px solid var(--surface-border)', opacity: currentPage === totalPages ? 0.5 : 1 }}
              >
                <ChevronRight size={20} />
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
};

export default History;
