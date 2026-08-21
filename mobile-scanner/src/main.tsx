import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

// Globally capture beforeinstallprompt as early as possible
window.addEventListener('beforeinstallprompt', (e) => {
  e.preventDefault();
  (window as any).deferredInstallPrompt = e;
  window.dispatchEvent(new Event('pwa-installable'));
});

// Register Service Worker for PWA installability
if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch((error) => {
      console.log('SW registration failed: ', error);
    });
  });
}

createRoot(document.getElementById('root')!).render(
  <App />
)

