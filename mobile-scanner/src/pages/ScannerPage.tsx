import { useEffect, useRef, useState } from 'react';
import { Html5Qrcode, Html5QrcodeSupportedFormats, type CameraDevice } from 'html5-qrcode';
import { useNavigate } from 'react-router-dom';
import { Camera, AlertCircle, RefreshCw, SwitchCamera } from 'lucide-react';

const ScannerPage = () => {
  const navigate = useNavigate();
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);
  const [cameras, setCameras] = useState<CameraDevice[]>([]);
  const [selectedCameraId, setSelectedCameraId] = useState<string | null>(null);
  const scannerRef = useRef<Html5Qrcode | null>(null);
  const scannedRef = useRef(false);

  const startScannerWithCamera = async (scanner: Html5Qrcode, cameraIdOrConfig: any) => {
    setIsInitializing(true);
    setErrorMsg(null);

    const qrCodeSuccessCallback = async (decodedText: string) => {
      if (scannedRef.current) return;
      scannedRef.current = true;

      try {
        if (scannerRef.current && scannerRef.current.isScanning) {
          await scannerRef.current.stop();
        }
      } catch {
        // ignore cleanup
      }

      let code = decodedText.trim();
      if (code.includes('/asset/')) {
        code = code.split('/asset/').pop() || code;
      }
      navigate(`/asset/${code}`);
    };

    const config = {
      fps: 15,
      qrbox: { width: 260, height: 260 },
      aspectRatio: 1.0
    };

    try {
      if (scanner.isScanning) {
        await scanner.stop();
      }
      await scanner.start(cameraIdOrConfig, config, qrCodeSuccessCallback, () => {});
      setIsInitializing(false);
    } catch (err: any) {
      console.warn("Gagal membuka kamera:", err);
      // Fallback
      try {
        await scanner.start({ facingMode: "environment" }, config, qrCodeSuccessCallback, () => {});
        setIsInitializing(false);
      } catch (fallbackErr: any) {
        console.error("Gagal total membuka kamera:", fallbackErr);
        setErrorMsg("Tidak dapat mengakses kamera belakang. Pastikan izin kamera telah diizinkan di browser.");
        setIsInitializing(false);
      }
    }
  };

  useEffect(() => {
    const qrRegionId = "qr-reader-direct";
    scannedRef.current = false;

    const html5QrCode = new Html5Qrcode(qrRegionId, {
      formatsToSupport: [Html5QrcodeSupportedFormats.QR_CODE],
      verbose: false
    });
    scannerRef.current = html5QrCode;

    const initCamera = async () => {
      try {
        const devices = await Html5Qrcode.getCameras();
        if (devices && devices.length > 0) {
          setCameras(devices);

          // Cari kamera belakang berdasarkan label kata kunci
          const rearCam = devices.find(d => 
            /back|rear|environment|belakang|utama|primary|0/i.test(d.label)
          ) || devices[devices.length - 1]; // Kamera belakang biasanya berada di indeks terakhir pada Android

          const targetId = rearCam ? rearCam.id : devices[0].id;
          setSelectedCameraId(targetId);
          await startScannerWithCamera(html5QrCode, targetId);
        } else {
          // Jika daftar kamera kosong, gunakan facingMode constraint
          await startScannerWithCamera(html5QrCode, { facingMode: "environment" });
        }
      } catch (e: any) {
        console.warn("getCameras() error, fallback to facingMode:", e);
        await startScannerWithCamera(html5QrCode, { facingMode: "environment" });
      }
    };

    initCamera();

    return () => {
      if (scannerRef.current) {
        if (scannerRef.current.isScanning) {
          scannerRef.current.stop().catch(() => {}).finally(() => {
            scannerRef.current?.clear();
          });
        } else {
          scannerRef.current.clear();
        }
      }
    };
  }, [navigate]);

  const handleSwitchCamera = async () => {
    if (!scannerRef.current || cameras.length < 2) return;

    const currentIndex = cameras.findIndex(c => c.id === selectedCameraId);
    const nextIndex = (currentIndex + 1) % cameras.length;
    const nextCamera = cameras[nextIndex];

    setSelectedCameraId(nextCamera.id);
    await startScannerWithCamera(scannerRef.current, nextCamera.id);
  };

  return (
    <div className="page-container animate-slide-up" style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minHeight: '85vh' }}>
      <header style={{ width: '100%', textAlign: 'center', marginBottom: 'var(--spacing-lg)' }}>
        <h1 style={{ fontSize: '24px', fontWeight: 700 }}>Pindai QR Aset</h1>
        <p style={{ color: 'var(--text-secondary)', fontSize: '14px', marginTop: '6px' }}>
          Arahkan kamera langsung ke label QR aset desa
        </p>
      </header>

      {/* Camera Viewport Container */}
      <div style={{ 
        position: 'relative', 
        width: '100%', 
        maxWidth: '340px', 
        borderRadius: '24px', 
        overflow: 'hidden',
        background: '#0f172a',
        boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.3), 0 8px 10px -6px rgba(0, 0, 0, 0.3)',
        aspectRatio: '1/1',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center'
      }}>
        {/* The video element container */}
        <div id="qr-reader-direct" style={{ width: '100%', height: '100%' }}></div>

        {/* Viewfinder Target Overlays */}
        {!errorMsg && !isInitializing && (
          <div style={{
            position: 'absolute',
            inset: '30px',
            border: '2px dashed rgba(16, 185, 129, 0.6)',
            borderRadius: '16px',
            pointerEvents: 'none',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}>
            {/* Corner Reticle Accents */}
            <div style={{ position: 'absolute', top: -2, left: -2, width: 24, height: 24, borderTop: '4px solid #10b981', borderLeft: '4px solid #10b981', borderTopLeftRadius: 12 }} />
            <div style={{ position: 'absolute', top: -2, right: -2, width: 24, height: 24, borderTop: '4px solid #10b981', borderRight: '4px solid #10b981', borderTopRightRadius: 12 }} />
            <div style={{ position: 'absolute', bottom: -2, left: -2, width: 24, height: 24, borderBottom: '4px solid #10b981', borderLeft: '4px solid #10b981', borderBottomLeftRadius: 12 }} />
            <div style={{ position: 'absolute', bottom: -2, right: -2, width: 24, height: 24, borderBottom: '4px solid #10b981', borderRight: '4px solid #10b981', borderBottomRightRadius: 12 }} />
            
            {/* Pulsing Scan Line */}
            <div style={{
              position: 'absolute',
              width: '100%',
              height: '2px',
              background: 'linear-gradient(90deg, transparent, #10b981, transparent)',
              boxShadow: '0 0 12px #10b981',
              animation: 'scanAnim 2s ease-in-out infinite'
            }} />
          </div>
        )}

        {/* Loading Spinner State */}
        {isInitializing && (
          <div style={{ position: 'absolute', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px', color: '#94a3b8' }}>
            <div style={{
              width: '36px', height: '36px', border: '3px solid #334155',
              borderTopColor: '#10b981', borderRadius: '50%', animation: 'spin 1s linear infinite'
            }} />
            <span style={{ fontSize: '13px' }}>Membuka kamera belakang...</span>
          </div>
        )}

        {/* Error Fallback Card */}
        {errorMsg && (
          <div style={{ 
            position: 'absolute', inset: 0, background: '#0f172a', padding: '24px', 
            display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', textAlign: 'center', color: 'white' 
          }}>
            <AlertCircle size={44} color="#ef4444" style={{ marginBottom: '12px' }} />
            <p style={{ fontSize: '14px', color: '#cbd5e1', marginBottom: '16px', lineHeight: 1.5 }}>
              {errorMsg}
            </p>
            <button 
              onClick={() => window.location.reload()}
              style={{
                display: 'flex', alignItems: 'center', gap: '8px',
                background: '#10b981', color: 'white', border: 'none',
                padding: '10px 18px', borderRadius: '9999px', fontWeight: 600, fontSize: '13px', cursor: 'pointer'
              }}
            >
              <RefreshCw size={16} />
              Coba Lagi
            </button>
          </div>
        )}
      </div>

      {/* Camera Switcher Button (if device has multiple cameras) */}
      {cameras.length > 1 && !errorMsg && !isInitializing && (
        <button
          onClick={handleSwitchCamera}
          style={{
            marginTop: '16px',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            background: 'var(--surface-color)',
            color: 'var(--text-primary)',
            border: '1px solid var(--surface-border)',
            padding: '8px 16px',
            borderRadius: '9999px',
            fontSize: '13px',
            fontWeight: 600,
            cursor: 'pointer',
            boxShadow: '0 4px 12px rgba(0,0,0,0.05)'
          }}
        >
          <SwitchCamera size={16} color="var(--accent-color)" />
          Ganti Lensa Kamera ({cameras.findIndex(c => c.id === selectedCameraId) + 1}/{cameras.length})
        </button>
      )}

      {/* Quick Tip for Village Staff */}
      <div style={{ marginTop: cameras.length > 1 ? '12px' : 'var(--spacing-xl)', textAlign: 'center', maxWidth: '300px' }}>
        <p style={{ fontSize: '13px', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px' }}>
          <Camera size={16} color="var(--accent-color)" />
          Kamera otomatis mendeteksi QR code
        </p>
      </div>

      <style>{`
        @keyframes scanAnim {
          0%, 100% { top: 10%; opacity: 0.2; }
          50% { top: 90%; opacity: 1; }
        }
        @keyframes spin { to { transform: rotate(360deg); } }
        #qr-reader-direct video {
          object-fit: cover !important;
          width: 100% !important;
          height: 100% !important;
          border-radius: 24px;
        }
      `}</style>
    </div>
  );
};

export default ScannerPage;

