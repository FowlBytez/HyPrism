import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from 'react';
import { ipc } from '../lib/ipc';
import type { ProgressUpdate, GameState, GameError } from '../lib/ipc';

interface GameContextType {
  isLaunching: boolean;
  isPlaying: boolean;
  isDownloading: boolean;
  progress: ProgressUpdate | null;
  error: GameError | null;
  launch: () => void;
  cancel: () => void;
  clearError: () => void;
}

const GameContext = createContext<GameContextType | null>(null);

export function GameProvider({ children }: { children: ReactNode }) {
  const [isLaunching, setIsLaunching] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);
  const [isDownloading, setIsDownloading] = useState(false);
  const [progress, setProgress] = useState<ProgressUpdate | null>(null);
  const [error, setError] = useState<GameError | null>(null);

  useEffect(() => {
    const cleanupProgress = ipc.game.onProgress((data) => {
      const p = (typeof data === 'string' ? JSON.parse(data as string) : data) as ProgressUpdate;
      setProgress(p);
      setIsDownloading(p.state !== 'complete' && p.state !== 'idle');
      if (p.state === 'complete') setIsLaunching(false);
    });

    const cleanupState = ipc.game.onState((data) => {
      const s = (typeof data === 'string' ? JSON.parse(data as string) : data) as GameState;
      switch (s.state) {
        case 'starting':
          setIsLaunching(true);
          setIsPlaying(false);
          break;
        case 'running':
          setIsLaunching(false);
          setIsPlaying(true);
          break;
        case 'stopped':
          setIsLaunching(false);
          setIsPlaying(false);
          setIsDownloading(false);
          break;
      }
    });

    const cleanupError = ipc.game.onError((data) => {
      const e = (typeof data === 'string' ? JSON.parse(data as string) : data) as GameError;
      setError(e);
      setIsLaunching(false);
      setIsDownloading(false);
    });

    return () => {
      cleanupProgress();
      cleanupState();
      cleanupError();
    };
  }, []);

  const launch = useCallback(() => {
    setError(null);
    setIsLaunching(true);
    ipc.game.launch();
  }, []);

  const cancel = useCallback(() => {
    ipc.game.cancel();
    setIsDownloading(false);
    setIsLaunching(false);
  }, []);

  const clearError = useCallback(() => setError(null), []);

  return (
    <GameContext.Provider
      value={{ isLaunching, isPlaying, isDownloading, progress, error, launch, cancel, clearError }}
    >
      {children}
    </GameContext.Provider>
  );
}

export function useGame() {
  const ctx = useContext(GameContext);
  if (!ctx) throw new Error('useGame must be used within GameProvider');
  return ctx;
}
