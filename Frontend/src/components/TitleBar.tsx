import { Minus, Square, X } from 'lucide-react';
import { ipc } from '../lib/ipc';

export function TitleBar() {
  return (
    <header
      className="flex items-center justify-between h-[var(--titlebar-height)] px-4 select-none z-50"
      style={{
        backgroundColor: 'var(--bg-dark)',
        borderBottom: '1px solid var(--glass-border)',
        // @ts-expect-error Electron CSS prop
        WebkitAppRegion: 'drag',
      }}
    >
      {/* Logo + version */}
      <div className="flex items-center gap-2.5">
        <div
          className="w-5 h-5 rounded-md flex items-center justify-center text-[10px] font-black"
          style={{ background: 'linear-gradient(135deg, var(--accent), #a78bfa)', color: 'white' }}
        >
          H
        </div>
        <span className="text-sm font-semibold tracking-wide" style={{ color: 'var(--text-primary)' }}>
          HyPrism
        </span>
        <span
          className="text-[10px] px-1.5 py-0.5 rounded-full font-medium"
          style={{ backgroundColor: 'var(--accent-subtle)', color: 'var(--accent)' }}
        >
          3.0
        </span>
      </div>

      {/* Window controls */}
      <div className="flex items-center -mr-1" style={{ WebkitAppRegion: 'no-drag' } as React.CSSProperties}>
        {[
          { icon: Minus, action: () => ipc.windowCtl.minimize(), hoverBg: 'hover:bg-white/8', title: 'Minimize' },
          { icon: Square, action: () => ipc.windowCtl.maximize(), hoverBg: 'hover:bg-white/8', title: 'Maximize' },
          { icon: X, action: () => ipc.windowCtl.close(), hoverBg: 'hover:bg-red-500/80', title: 'Close' },
        ].map(({ icon: Icon, action, hoverBg, title }) => (
          <button
            key={title}
            onClick={action}
            className={`w-10 h-[var(--titlebar-height)] flex items-center justify-center transition-colors ${hoverBg}`}
            title={title}
          >
            <Icon size={title === 'Maximize' ? 11 : 13} style={{ color: 'var(--text-secondary)' }} />
          </button>
        ))}
      </div>
    </header>
  );
}
