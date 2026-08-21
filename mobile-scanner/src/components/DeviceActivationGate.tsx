import React, { useState, useEffect } from 'react';
import { ShieldCheck, AlertCircle, CheckCircle2, ArrowRight } from 'lucide-react';
import { supabase } from '../lib/supabaseClient';

interface DeviceActivationGateProps {
  children: React.ReactNode;
}

export const DeviceActivationGate: React.FC<DeviceActivationGateProps> = ({ children }) => {
  const [isAuthorized, setIsAuthorized] = useState<boolean | null>(null);
  const [pin, setPin] = useState('');
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    // Check if device is already activated
    const authStatus = localStorage.getItem('desa_device_authorized');
    if (authStatus === 'true') {
      setIsAuthorized(true);
    } else {
      setIsAuthorized(false);
    }
  }, []);

  const handleVerifyPin = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!pin || pin.trim().length < 4) {
      setErrorMsg('Masukkan minimal 4 digit PIN.');
      return;
    }

    setLoading(true);
    setErrorMsg('');

    try {
      // 1. Fetch staff_pin from Supabase system_settings
      const { data, error } = await supabase
        .from('system_settings')
        .select('value')
        .eq('key', 'staff_pin')
        .maybeSingle();

      let correctPin = '123456'; // Default fallback
      if (!error && data && data.value) {
        correctPin = data.value.trim();
      }

      if (pin.trim() === correctPin) {
        setSuccess(true);
        localStorage.setItem('desa_device_authorized', 'true');
        setTimeout(() => {
          setIsAuthorized(true);
        }, 1200);
      } else {
        setErrorMsg('PIN Keamanan Salah. Hubungi Admin Balai Desa.');
        setPin('');
      }
    } catch (err: any) {
      // If offline or network error, check fallback PIN
      if (pin.trim() === '123456') {
        setSuccess(true);
        localStorage.setItem('desa_device_authorized', 'true');
        setTimeout(() => {
          setIsAuthorized(true);
        }, 1000);
      } else {
        setErrorMsg('Gagal memverifikasi PIN. Periksa koneksi internet.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleNumClick = (num: string) => {
    if (pin.length < 8) {
      setPin((prev) => prev + num);
      setErrorMsg('');
    }
  };

  const handleDelete = () => {
    setPin((prev) => prev.slice(0, -1));
    setErrorMsg('');
  };

  // If still checking initial status
  if (isAuthorized === null) {
    return (
      <div style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: '#0F172A',
        color: 'white'
      }}>
        <div style={{ textAlign: 'center' }}>
          <div style={{
            width: '40px',
            height: '40px',
            border: '3px solid #334155',
            borderTopColor: '#10B981',
            borderRadius: '50%',
            animation: 'spin 1s linear infinite',
            margin: '0 auto 16px'
          }} />
          <p style={{ fontSize: '13px', color: '#94A3B8' }}>Memeriksa Otorisasi Perangkat...</p>
        </div>
      </div>
    );
  }

  // If authorized, show the app immediately!
  if (isAuthorized) {
    return <>{children}</>;
  }

  // If not authorized, show the Activation Gate
  return (
    <div style={{
      minHeight: '100vh',
      background: 'linear-gradient(180deg, #0F172A 0%, #1E293B 100%)',
      color: '#F8FAFC',
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      padding: '24px 16px',
      fontFamily: 'system-ui, -apple-system, sans-serif'
    }}>
      <div style={{
        width: '100%',
        maxWidth: '380px',
        background: 'rgba(30, 41, 59, 0.8)',
        backdropFilter: 'blur(16px)',
        border: '1px solid rgba(255, 255, 255, 0.08)',
        borderRadius: '24px',
        padding: '28px 20px',
        boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.5)',
        textAlign: 'center'
      }}>
        {/* Header Badge */}
        <div style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: '6px',
          background: 'rgba(16, 185, 129, 0.12)',
          border: '1px solid rgba(16, 185, 129, 0.3)',
          padding: '6px 14px',
          borderRadius: '9999px',
          fontSize: '11px',
          fontWeight: 700,
          color: '#34D399',
          letterSpacing: '0.5px',
          textTransform: 'uppercase',
          marginBottom: '16px'
        }}>
          <ShieldCheck size={14} /> Balai Desa Pucang
        </div>

        {/* Title */}
        <h1 style={{
          fontSize: '20px',
          fontWeight: 800,
          margin: '0 0 6px',
          color: '#FFFFFF',
          letterSpacing: '-0.3px'
        }}>
          Aktivasi Perangkat Petugas
        </h1>

        <p style={{
          fontSize: '12px',
          color: '#94A3B8',
          margin: '0 0 24px',
          lineHeight: '1.5'
        }}>
          Masukkan PIN Keamanan untuk mengotorisasi smartphone ini sebelum dapat menginstal dan menggunakan aplikasi.
        </p>

        {/* PIN Dots Display */}
        <div style={{
          display: 'flex',
          justifyContent: 'center',
          gap: '12px',
          marginBottom: '20px'
        }}>
          {[0, 1, 2, 3, 4, 5].map((idx) => {
            const isFilled = pin.length > idx;
            return (
              <div
                key={idx}
                style={{
                  width: '14px',
                  height: '14px',
                  borderRadius: '50%',
                  background: isFilled ? '#10B981' : '#334155',
                  boxShadow: isFilled ? '0 0 12px rgba(16, 185, 129, 0.6)' : 'none',
                  transition: 'all 0.2s ease',
                  border: isFilled ? '2px solid #34D399' : '1px solid #475569'
                }}
              />
            );
          })}
        </div>

        {/* Status / Error Message */}
        {errorMsg && (
          <div style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '6px',
            background: 'rgba(239, 68, 68, 0.15)',
            border: '1px solid rgba(239, 68, 68, 0.3)',
            color: '#F87171',
            padding: '8px 12px',
            borderRadius: '12px',
            fontSize: '12px',
            fontWeight: 600,
            marginBottom: '16px'
          }}>
            <AlertCircle size={15} /> {errorMsg}
          </div>
        )}

        {success && (
          <div style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '6px',
            background: 'rgba(16, 185, 129, 0.2)',
            border: '1px solid rgba(16, 185, 129, 0.4)',
            color: '#34D399',
            padding: '10px 12px',
            borderRadius: '12px',
            fontSize: '12px',
            fontWeight: 700,
            marginBottom: '16px'
          }}>
            <CheckCircle2 size={16} /> Perangkat Terotorisasi! Membuka...
          </div>
        )}

        {/* Numeric Keypad */}
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(3, 1fr)',
          gap: '10px',
          marginBottom: '20px'
        }}>
          {['1', '2', '3', '4', '5', '6', '7', '8', '9', 'C', '0', '⌫'].map((item) => {
            if (item === 'C') {
              return (
                <button
                  key={item}
                  onClick={() => setPin('')}
                  style={{
                    background: 'rgba(51, 65, 85, 0.4)',
                    border: '1px solid rgba(255, 255, 255, 0.05)',
                    color: '#94A3B8',
                    padding: '14px 0',
                    borderRadius: '14px',
                    fontSize: '14px',
                    fontWeight: 700,
                    cursor: 'pointer'
                  }}
                >
                  C
                </button>
              );
            }
            if (item === '⌫') {
              return (
                <button
                  key={item}
                  onClick={handleDelete}
                  style={{
                    background: 'rgba(51, 65, 85, 0.4)',
                    border: '1px solid rgba(255, 255, 255, 0.05)',
                    color: '#94A3B8',
                    padding: '14px 0',
                    borderRadius: '14px',
                    fontSize: '14px',
                    fontWeight: 700,
                    cursor: 'pointer'
                  }}
                >
                  ⌫
                </button>
              );
            }
            return (
              <button
                key={item}
                onClick={() => handleNumClick(item)}
                style={{
                  background: 'rgba(51, 65, 85, 0.7)',
                  border: '1px solid rgba(255, 255, 255, 0.08)',
                  color: '#FFFFFF',
                  padding: '14px 0',
                  borderRadius: '14px',
                  fontSize: '18px',
                  fontWeight: 700,
                  cursor: 'pointer',
                  transition: 'background 0.15s ease'
                }}
              >
                {item}
              </button>
            );
          })}
        </div>

        {/* Submit Button */}
        <button
          onClick={() => handleVerifyPin()}
          disabled={loading || pin.length < 4}
          style={{
            width: '100%',
            background: pin.length >= 4 ? 'linear-gradient(135deg, #10B981, #059669)' : '#334155',
            color: pin.length >= 4 ? 'white' : '#64748B',
            border: 'none',
            padding: '14px',
            borderRadius: '14px',
            fontSize: '14px',
            fontWeight: 700,
            cursor: pin.length >= 4 ? 'pointer' : 'not-allowed',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '8px',
            boxShadow: pin.length >= 4 ? '0 10px 20px -5px rgba(16, 185, 129, 0.4)' : 'none',
            transition: 'all 0.2s ease'
          }}
        >
          {loading ? 'Memverifikasi...' : <>Aktifkan Smartphone Ini <ArrowRight size={16} /></>}
        </button>

        <p style={{
          fontSize: '11px',
          color: '#64748B',
          marginTop: '16px',
          marginBottom: 0
        }}>
          🔒 PIN hanya perlu dimasukkan 1 kali saat aktivasi.
        </p>
      </div>
    </div>
  );
};
