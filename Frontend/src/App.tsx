import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { GameProvider } from './contexts/GameContext';
import { TitleBar } from './components/TitleBar';
import { Sidebar } from './components/Sidebar';
import { PageTransition } from './components/PageTransition';
import { Dashboard } from './pages/Dashboard';
import { News } from './pages/News';
import { Settings } from './pages/Settings';
import { ModManager } from './pages/ModManager';

export default function App() {
  return (
    <BrowserRouter>
      <GameProvider>
        <div className="flex flex-col h-screen overflow-hidden" style={{ backgroundColor: 'var(--bg-darkest)' }}>
          <TitleBar />
          <div className="flex flex-1 overflow-hidden">
            <Sidebar />
            <main className="flex-1 overflow-hidden relative">
              <PageTransition>
                <Routes>
                  <Route path="/" element={<Dashboard />} />
                  <Route path="/news" element={<News />} />
                  <Route path="/mods" element={<ModManager />} />
                  <Route path="/settings" element={<Settings />} />
                </Routes>
              </PageTransition>
            </main>
          </div>
        </div>
      </GameProvider>
    </BrowserRouter>
  );
}
