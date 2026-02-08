import { useRef, useEffect } from 'react';
import { Play, Square, Loader2, Gamepad2, Zap, ChevronRight } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import gsap from 'gsap';
import { useGame } from '../contexts/GameContext';
import { GlassCard } from '../components/GlassCard';

export function Dashboard() {
  const { isLaunching, isPlaying, isDownloading, progress, error, launch, cancel, clearError } = useGame();
  const heroRef = useRef<HTMLDivElement>(null);
  const btnRef = useRef<HTMLButtonElement>(null);
  const navigate = useNavigate();

  // Hero reveal + floating animation
  useEffect(() => {
    if (!heroRef.current) return;
    const els = heroRef.current.querySelectorAll('.reveal');
    gsap.from(els, {
      opacity: 0,
      y: 40,
      stagger: 0.1,
      duration: 0.7,
      ease: 'power3.out',
      delay: 0.2,
    });
  }, []);

  // Play button pulse
  useEffect(() => {
    if (!btnRef.current || isLaunching || isPlaying || isDownloading) return;
    const pulse = gsap.to(btnRef.current, {
      boxShadow: '0 0 40px var(--accent-glow), 0 0 80px rgba(124,92,252,0.12)',
      duration: 1.6,
      repeat: -1,
      yoyo: true,
      ease: 'sine.inOut',
    });
    return () => { pulse.kill(); };
  }, [isLaunching, isPlaying, isDownloading]);

  const getButtonState = () => {
    if (isPlaying) return { text: 'Playing', icon: Square, disabled: true, variant: 'playing' as const };
    if (isDownloading) return { text: 'Cancel', icon: Square, disabled: false, variant: 'cancel' as const };
    if (isLaunching) return { text: 'Launching...', icon: Loader2, disabled: true, variant: 'launching' as const };
    return { text: 'Play', icon: Play, disabled: false, variant: 'play' as const };
  };

  const btn = getButtonState();

  return (
    <div ref={heroRef} className="flex flex-col h-full relative animated-bg">
      {/* Ambient glow orbs */}
      <div className="absolute top-1/4 left-1/3 w-96 h-96 rounded-full pointer-events-none"
        style={{ background: 'radial-gradient(circle, rgba(124,92,252,0.06) 0%, transparent 70%)', filter: 'blur(60px)' }} />
      <div className="absolute bottom-1/4 right-1/4 w-64 h-64 rounded-full pointer-events-none"
        style={{ background: 'radial-gradient(circle, rgba(74,222,128,0.04) 0%, transparent 70%)', filter: 'blur(40px)' }} />

      {/* Header */}
      <div className="relative z-10 px-8 pt-7">
        <div className="flex items-start justify-between">
          <div className="reveal">
            <div className="flex items-center gap-3 mb-1">
              <Gamepad2 size={24} style={{ color: 'var(--accent)' }} />
              <h1 className="text-3xl font-bold tracking-tight" style={{ color: 'var(--text-primary)' }}>
                Hytale
              </h1>
            </div>
            <p className="text-sm ml-9" style={{ color: 'var(--text-secondary)' }}>
              Ready to play
            </p>
          </div>

          {/* Profile badge */}
          <GlassCard className="reveal px-4 py-2.5 flex items-center gap-3" hover={false} delay={0.15}>
            <div
              className="w-9 h-9 rounded-full flex items-center justify-center text-sm font-bold"
              style={{ background: 'linear-gradient(135deg, var(--accent), #a78bfa)', color: 'white' }}
            >
              P
            </div>
            <div>
              <div className="text-sm font-medium" style={{ color: 'var(--text-primary)' }}>Player</div>
              <div className="text-[11px]" style={{ color: 'var(--text-muted)' }}>Online</div>
            </div>
          </GlassCard>
        </div>
      </div>

      {/* Quick links */}
      <div className="relative z-10 px-8 mt-8 flex gap-3 reveal">
        {[
          { label: 'News', path: '/news', icon: Zap },
        ].map((link) => (
          <button
            key={link.path}
            onClick={() => navigate(link.path)}
            className="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-medium transition-all duration-200 hover:scale-[1.02]"
            style={{
              backgroundColor: 'var(--accent-subtle)',
              color: 'var(--accent)',
              border: '1px solid rgba(124,92,252,0.15)',
            }}
          >
            <link.icon size={14} />
            {link.label}
            <ChevronRight size={12} />
          </button>
        ))}
      </div>

      {/* Spacer */}
      <div className="flex-1" />

      {/* Error banner */}
      {error && (
        <div
          className="mx-8 mb-4 px-5 py-3 rounded-xl flex items-center justify-between reveal"
          style={{ backgroundColor: 'rgba(248,113,113,0.1)', border: '1px solid rgba(248,113,113,0.3)' }}
        >
          <div>
            <p className="text-sm font-medium" style={{ color: 'var(--error)' }}>
              {error.type}: {error.message}
            </p>
            {error.technical && (
              <p className="text-xs mt-1" style={{ color: 'var(--text-muted)' }}>{error.technical}</p>
            )}
          </div>
          <button
            onClick={clearError}
            className="text-xs px-3 py-1.5 rounded-lg transition-colors"
            style={{ color: 'var(--error)', backgroundColor: 'rgba(248,113,113,0.15)' }}
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Game control bar */}
      <div className="mx-8 mb-8 reveal">
        <GlassCard className="px-6 py-5 flex items-center justify-between" hover={false}>
          {/* Status */}
          <div className="flex flex-col gap-1">
            <span className="text-[10px] uppercase tracking-widest font-semibold" style={{ color: 'var(--text-muted)' }}>
              Status
            </span>
            <span className="text-sm font-medium" style={{ color: 'var(--text-primary)' }}>
              {isPlaying ? 'Game is running' :
               isDownloading ? `Downloading... ${progress?.progress ?? 0}%` :
               isLaunching ? 'Preparing...' :
               'Ready to play'}
            </span>
          </div>

          {/* Progress bar */}
          {isDownloading && progress && (
            <div className="flex-1 mx-8">
              <div className="h-1.5 rounded-full overflow-hidden" style={{ backgroundColor: 'var(--bg-lighter)' }}>
                <div
                  className="h-full rounded-full transition-all duration-300"
                  style={{
                    width: `${progress.progress}%`,
                    background: 'linear-gradient(90deg, var(--accent), #a78bfa)',
                    boxShadow: '0 0 12px var(--accent-glow)',
                  }}
                />
              </div>
              <div className="flex justify-between mt-1.5">
                <span className="text-[11px]" style={{ color: 'var(--text-muted)' }}>
                  {formatBytes(progress.downloadedBytes)} / {formatBytes(progress.totalBytes)}
                </span>
                <span className="text-[11px] font-medium" style={{ color: 'var(--accent)' }}>
                  {progress.progress}%
                </span>
              </div>
            </div>
          )}

          {/* Play button */}
          <button
            ref={btnRef}
            onClick={btn.variant === 'cancel' ? cancel : launch}
            disabled={btn.disabled}
            className="flex items-center gap-2.5 px-10 py-3.5 rounded-xl font-semibold text-sm transition-all duration-200"
            style={{
              background: btn.variant === 'cancel' ? 'var(--error)' :
                          btn.variant === 'playing' ? 'var(--success)' :
                          'linear-gradient(135deg, var(--accent), #6A4AE8)',
              color: 'white',
              opacity: btn.disabled ? 0.7 : 1,
              cursor: btn.disabled ? 'not-allowed' : 'pointer',
            }}
          >
            <btn.icon size={18} className={btn.variant === 'launching' ? 'animate-spin' : ''} />
            {btn.text}
          </button>
        </GlassCard>
      </div>
    </div>
  );
}

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(1)} ${sizes[i]}`;
}
