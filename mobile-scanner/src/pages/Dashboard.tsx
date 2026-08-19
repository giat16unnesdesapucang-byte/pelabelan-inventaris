import { useEffect, useState } from 'react';
import { Package, ArrowUpRight, ArrowDownRight, AlertCircle, Search, Clock } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { supabase } from '../lib/supabaseClient';

interface DashboardStats {
  totalAssets: number;
  borrowedAssets: number;
}

interface RecentActivity {
  id: string;
  asset_name: string;
  borrower: string;
  action: 'Dipinjam' | 'Dikembalikan';
  time: string;
}

interface OverdueItem {
  id: string;
  asset_code: string;
  asset_name: string;
  borrower: string;
  expected_return: string;
}

const Dashboard = () => {
  const navigate = useNavigate();
  const [stats, setStats] = useState<DashboardStats>({ totalAssets: 0, borrowedAssets: 0 });
  const [activities, setActivities] = useState<RecentActivity[]>([]);
  const [overdue, setOverdue] = useState<OverdueItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchCode, setSearchCode] = useState('');

  useEffect(() => {
    const fetchData = async () => {
      try {
        const { count: totalCount } = await supabase
          .from('assets')
          .select('*', { count: 'exact', head: true });
        
        const { count: borrowedCount } = await supabase
          .from('assets')
          .select('*', { count: 'exact', head: true })
          .eq('availability_status', 'Dipinjam');

        setStats({
          totalAssets: totalCount || 0,
          borrowedAssets: borrowedCount || 0
        });

        const { data: recentData } = await supabase
          .from('loan_transactions')
          .select('id, status, borrower_name, borrow_date, actual_return_date, assets(name)')
          .order('updated_at', { ascending: false })
          .limit(5);

        if (recentData) {
          const formattedActivities = recentData.map((item: any) => {
            const isReturned = item.status === 'Selesai';
            const actionTime = isReturned ? item.actual_return_date : item.borrow_date;
            const date = new Date(actionTime);
            return {
              id: item.id,
              asset_name: item.assets?.name || 'Aset Tidak Diketahui',
              borrower: item.borrower_name,
              action: isReturned ? 'Dikembalikan' : 'Dipinjam',
              time: date.toLocaleDateString('id-ID', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
            } as RecentActivity;
          });
          setActivities(formattedActivities);
        }

        const now = new Date().toISOString();
        const { data: overdueData } = await supabase
          .from('loan_transactions')
          .select('id, expected_return_date, borrower_name, assets(asset_code, name)')
          .eq('status', 'Aktif')
          .lt('expected_return_date', now)
          .limit(3);

        if (overdueData) {
          setOverdue(overdueData.map((item: any) => ({
            id: item.id,
            asset_code: item.assets?.asset_code,
            asset_name: item.assets?.name,
            borrower: item.borrower_name,
            expected_return: new Date(item.expected_return_date).toLocaleDateString('id-ID', { day: 'numeric', month: 'short' })
          })));
        }

      } catch (err) {
        console.error('Error fetching dashboard data:', err);
      } finally {
        setLoading(false);
      }
    };
    
    fetchData();
  }, []);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchCode.trim()) {
      navigate(`/asset/${searchCode.trim()}`);
    }
  };

  if (loading) {
    return (
      <div className="page-container" style={{ justifyContent: 'center', alignItems: 'center' }}>
        <div style={{
          width: '40px', height: '40px', border: '3px solid var(--surface-border)',
          borderTopColor: 'var(--accent-color)', borderRadius: '50%', animation: 'spin 1s linear infinite'
        }} />
        <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
      </div>
    );
  }

  return (
    <div className="page-container animate-slide-up" style={{ paddingBottom: '120px' }}>
      <header style={{ marginBottom: 'var(--spacing-md)' }}>
        <p style={{ color: 'var(--text-secondary)', fontSize: '14px', fontWeight: 500, marginBottom: '4px' }}>
          Balai Desa Pucang
        </p>
        <h1 style={{ fontSize: '28px', lineHeight: 1.2 }}>Beranda Petugas</h1>
      </header>

      {/* Hero Action */}
      <div style={{ marginBottom: 'var(--spacing-lg)' }}>
        <form onSubmit={handleSearch} style={{ display: 'flex', gap: '8px' }}>
          <div style={{ 
            flex: 1, display: 'flex', alignItems: 'center', gap: '8px', background: 'var(--surface-color)', 
            padding: '12px 16px', borderRadius: 'var(--radius-md)', border: '1px solid var(--surface-border)' 
          }}>
            <Search size={18} color="var(--text-secondary)" />
            <input 
              type="text" 
              placeholder="Atau masukkan ID manual (INV-...)" 
              value={searchCode}
              onChange={(e) => setSearchCode(e.target.value)}
              style={{ border: 'none', background: 'transparent', outline: 'none', width: '100%', fontSize: '14px', color: 'var(--text-primary)' }}
            />
          </div>
        </form>
      </div>

      {/* Stats Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 'var(--spacing-md)', marginBottom: 'var(--spacing-lg)' }}>
        <div className="glass-panel" style={{ padding: 'var(--spacing-md)' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px', color: 'var(--text-secondary)' }}>
            <Package size={16} />
            <span style={{ fontSize: '12px', fontWeight: 600 }}>Total Aset</span>
          </div>
          <div style={{ fontSize: '24px', fontFamily: 'var(--font-display)', fontWeight: 700 }}>
            {stats.totalAssets}
          </div>
        </div>

        <div className="glass-panel" style={{ padding: 'var(--spacing-md)', background: 'var(--accent-light)', borderColor: 'var(--accent-color)' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px', color: 'var(--accent-hover)' }}>
            <ArrowUpRight size={16} />
            <span style={{ fontSize: '12px', fontWeight: 600 }}>Dipinjam</span>
          </div>
          <div style={{ fontSize: '24px', fontFamily: 'var(--font-display)', fontWeight: 700, color: 'var(--accent-hover)' }}>
            {stats.borrowedAssets}
          </div>
        </div>
      </div>

      {/* Overdue Alerts */}
      {overdue.length > 0 && (
        <section style={{ marginBottom: 'var(--spacing-lg)' }}>
          <h2 style={{ fontSize: '16px', marginBottom: '12px', display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--status-bad)' }}>
            <AlertCircle size={18} />
            Terlambat Dikembalikan
          </h2>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            {overdue.map(item => (
              <div key={item.id} className="glass-panel" style={{ padding: '12px 16px', borderLeft: '4px solid var(--status-bad)' }}>
                <p style={{ fontWeight: 600, fontSize: '14px' }}>{item.asset_name}</p>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '4px', fontSize: '12px', color: 'var(--text-secondary)' }}>
                  <span>Oleh: {item.borrower}</span>
                  <span style={{ color: 'var(--status-bad)', fontWeight: 600 }}>Tenggat: {item.expected_return}</span>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* Recent Activities */}
      <section>
        <h2 style={{ fontSize: '16px', marginBottom: '12px', display: 'flex', alignItems: 'center', gap: '8px' }}>
          <Clock size={18} color="var(--text-secondary)" />
          Aktivitas Terakhir
        </h2>
        
        {activities.length === 0 ? (
          <div className="glass-panel" style={{ padding: '24px', textAlign: 'center', color: 'var(--text-secondary)' }}>
            <p style={{ fontSize: '14px' }}>Belum ada aktivitas terekam.</p>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            {activities.map((activity) => (
              <div key={activity.id} className="glass-panel" style={{ 
                padding: '12px 16px', display: 'flex', alignItems: 'center', gap: '16px' 
              }}>
                <div style={{
                  width: '40px', height: '40px', borderRadius: '50%',
                  background: activity.action === 'Dipinjam' ? 'rgba(59, 130, 246, 0.1)' : 'var(--accent-light)',
                  color: activity.action === 'Dipinjam' ? '#3B82F6' : 'var(--accent-color)',
                  display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0
                }}>
                  {activity.action === 'Dipinjam' ? <ArrowUpRight size={20} /> : <ArrowDownRight size={20} />}
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <p style={{ fontWeight: 600, fontSize: '14px', marginBottom: '2px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {activity.asset_name}
                  </p>
                  <p style={{ fontSize: '12px', color: 'var(--text-secondary)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {activity.action} oleh {activity.borrower}
                  </p>
                </div>
                <div style={{ fontSize: '11px', color: 'var(--text-secondary)', fontWeight: 500, textAlign: 'right' }}>
                  {activity.time}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
};

export default Dashboard;
