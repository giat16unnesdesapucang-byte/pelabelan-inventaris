import { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Link, useLocation } from 'react-router-dom';
import { Home, ScanLine, Clock } from 'lucide-react';
import Dashboard from './pages/Dashboard';
import ScannerPage from './pages/ScannerPage';
import AssetDetails from './pages/AssetDetails';
import History from './pages/History';
import './index.css';

const InstallPromptBanner = () => {
  const [deferredPrompt, setDeferredPrompt] = useState<any>(null);
  const [showBanner, setShowBanner] = useState(false);

  useEffect(() => {
    const isStandalone = window.matchMedia('(display-mode: standalone)').matches || (window.navigator as any).standalone;
    if (isStandalone) return;

    const handler = (e: any) => {
      e.preventDefault();
      setDeferredPrompt(e);
      setShowBanner(true);
    };

    window.addEventListener('beforeinstallprompt', handler);
    return () => window.removeEventListener('beforeinstallprompt', handler);
  }, []);

  const handleInstall = async () => {
    if (!deferredPrompt) return;
    deferredPrompt.prompt();
    const { outcome } = await deferredPrompt.userChoice;
    if (outcome === 'accepted') {
      setShowBanner(false);
    }
    setDeferredPrompt(null);
  };

  if (!showBanner) return null;

  return (
    <div style={{
      position: 'fixed',
      top: '12px',
      left: '12px',
      right: '12px',
      maxWidth: '456px',
      margin: '0 auto',
      background: 'linear-gradient(135deg, #10b981, #059669)',
      color: 'white',
      padding: '12px 16px',
      borderRadius: '16px',
      boxShadow: '0 10px 25px -5px rgba(16, 185, 129, 0.4)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      zIndex: 100
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
        <span style={{ fontSize: '22px' }}>📲</span>
        <div>
          <p style={{ fontSize: '13px', fontWeight: 700, margin: 0 }}>Pasang Aplikasi Aset Desa</p>
          <p style={{ fontSize: '11px', opacity: 0.9, margin: 0 }}>Akses lebih cepat &amp; layar penuh</p>
        </div>
      </div>
      <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
        <button
          onClick={handleInstall}
          style={{
            background: 'white',
            color: '#059669',
            border: 'none',
            padding: '6px 14px',
            borderRadius: '9999px',
            fontSize: '12px',
            fontWeight: 700,
            cursor: 'pointer'
          }}
        >
          Pasang
        </button>
        <button
          onClick={() => setShowBanner(false)}
          style={{
            background: 'transparent',
            color: 'white',
            border: 'none',
            fontSize: '14px',
            cursor: 'pointer',
            opacity: 0.8,
            padding: '4px 8px'
          }}
        >
          ✕
        </button>
      </div>
    </div>
  );
};

const BottomNav = () => {
  const location = useLocation();
  const isActive = (path: string) => location.pathname === path;

  return (
    <nav style={{
      position: 'fixed',
      bottom: 0,
      left: 0,
      right: 0,
      height: '80px',
      background: 'var(--surface-color)',
      backdropFilter: 'blur(20px)',
      borderTop: '1px solid var(--surface-border)',
      display: 'flex',
      justifyContent: 'space-around',
      alignItems: 'center',
      paddingBottom: 'env(safe-area-inset-bottom)',
      zIndex: 50,
      maxWidth: '480px',
      margin: '0 auto',
    }}>
      <Link to="/" style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '4px', color: isActive('/') ? 'var(--accent-color)' : 'var(--text-secondary)' }}>
        <Home size={24} strokeWidth={isActive('/') ? 2.5 : 2} />
        <span style={{ fontSize: '11px', fontWeight: isActive('/') ? 600 : 500 }}>Beranda</span>
      </Link>
      
      <Link to="/scan" style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        width: '64px',
        height: '64px',
        background: 'var(--accent-color)',
        borderRadius: '50%',
        color: 'white',
        transform: 'translateY(-20px)',
        boxShadow: '0 8px 24px rgba(16, 185, 129, 0.4)',
        border: '4px solid var(--bg-color)',
      }}>
        <ScanLine size={28} />
      </Link>
      
      <Link to="/history" style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '4px', color: isActive('/history') ? 'var(--accent-color)' : 'var(--text-secondary)' }}>
        <Clock size={24} strokeWidth={isActive('/history') ? 2.5 : 2} />
        <span style={{ fontSize: '11px', fontWeight: isActive('/history') ? 600 : 500 }}>Riwayat</span>
      </Link>
    </nav>
  );
};

function App() {
  return (
    <Router>
      <div id="app">
        <InstallPromptBanner />
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/scan" element={<ScannerPage />} />
          <Route path="/asset/:id" element={<AssetDetails />} />
          <Route path="/history" element={<History />} />
        </Routes>
        <BottomNav />
      </div>
    </Router>
  );
}

export default App;

