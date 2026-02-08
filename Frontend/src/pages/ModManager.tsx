import { useState, useEffect, useRef, useCallback } from 'react';
import {
  Package, Search, Download, Trash2, RefreshCw,
  Star, AlertTriangle,
} from 'lucide-react';
import gsap from 'gsap';
import { ipc } from '../lib/ipc';
import type { ModItem } from '../lib/ipc';
import { GlassCard } from '../components/GlassCard';

type Tab = 'installed' | 'browse';

export function ModManager() {
  const [tab, setTab] = useState<Tab>('installed');
  const [installedMods, setInstalledMods] = useState<ModItem[]>([]);
  const [browseMods, setBrowseMods] = useState<ModItem[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(false);
  const [searching, setSearching] = useState(false);
  const gridRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);

  // Load installed mods on mount
  useEffect(() => {
    loadInstalled();
  }, []);

  // Animate cards on tab/data change
  useEffect(() => {
    if (!gridRef.current) return;
    const cards = gridRef.current.querySelectorAll('.mod-card');
    if (cards.length === 0) return;
    gsap.from(cards, {
      opacity: 0,
      y: 20,
      scale: 0.97,
      stagger: 0.04,
      duration: 0.4,
      ease: 'power2.out',
    });
  }, [tab, installedMods, browseMods]);

  const loadInstalled = async () => {
    setLoading(true);
    try {
      const mods = await ipc.mods.list();
      setInstalledMods(mods ?? []);
    } catch {
      setInstalledMods([]);
    } finally {
      setLoading(false);
    }
  };

  const searchMods = useCallback(async (query: string) => {
    if (!query.trim()) {
      setBrowseMods([]);
      return;
    }
    setSearching(true);
    try {
      const result = await ipc.mods.search(query);
      setBrowseMods(result?.items ?? []);
    } catch {
      setBrowseMods([]);
    } finally {
      setSearching(false);
    }
  }, []);

  // Debounced search
  useEffect(() => {
    if (tab !== 'browse') return;
    const timer = setTimeout(() => searchMods(searchQuery), 400);
    return () => clearTimeout(timer);
  }, [searchQuery, tab, searchMods]);

  const currentMods = tab === 'installed' ? installedMods : browseMods;

  return (
    <div className="flex flex-col h-full overflow-y-auto custom-scrollbar">
      {/* Header */}
      <div className="px-8 pt-7 pb-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div
              className="w-10 h-10 rounded-xl flex items-center justify-center"
              style={{ background: 'linear-gradient(135deg, var(--accent), #a78bfa)', boxShadow: '0 4px 15px var(--accent-glow)' }}
            >
              <Package size={20} color="white" />
            </div>
            <div>
              <h1 className="text-2xl font-bold" style={{ color: 'var(--text-primary)' }}>Mods</h1>
              <p className="text-xs" style={{ color: 'var(--text-muted)' }}>
                {installedMods.length} installed
              </p>
            </div>
          </div>

          <button
            onClick={loadInstalled}
            disabled={loading}
            className="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-medium transition-all duration-200 hover:scale-105"
            style={{ backgroundColor: 'var(--bg-light)', color: 'var(--text-secondary)', border: '1px solid var(--glass-border)' }}
          >
            <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            Refresh
          </button>
        </div>

        {/* Tabs */}
        <div className="flex gap-1 mt-5 p-1 rounded-xl" style={{ backgroundColor: 'var(--bg-dark)' }}>
          {(['installed', 'browse'] as Tab[]).map((t) => (
            <button
              key={t}
              onClick={() => setTab(t)}
              className="flex-1 py-2 rounded-lg text-xs font-semibold transition-all duration-200 capitalize"
              style={{
                backgroundColor: tab === t ? 'var(--bg-light)' : 'transparent',
                color: tab === t ? 'var(--text-primary)' : 'var(--text-muted)',
                boxShadow: tab === t ? '0 2px 8px rgba(0,0,0,0.3)' : 'none',
              }}
            >
              {t === 'installed' ? `Installed (${installedMods.length})` : 'Browse'}
            </button>
          ))}
        </div>

        {/* Search (visible in browse tab) */}
        {tab === 'browse' && (
          <div className="mt-4 relative">
            <Search size={16}
              className="absolute left-3 top-1/2 -translate-y-1/2"
              style={{ color: 'var(--text-muted)' }}
            />
            <input
              ref={searchRef}
              type="text"
              placeholder="Search mods on CurseForge..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2.5 rounded-xl text-sm outline-none transition-all duration-200"
              style={{
                backgroundColor: 'var(--bg-medium)',
                color: 'var(--text-primary)',
                border: '1px solid var(--glass-border)',
              }}
              onFocus={(e) => {
                gsap.to(e.target, { borderColor: 'var(--accent)', duration: 0.2 });
              }}
              onBlur={(e) => {
                gsap.to(e.target, { borderColor: 'var(--glass-border)', duration: 0.2 });
              }}
            />
            {searching && (
              <RefreshCw size={14}
                className="absolute right-3 top-1/2 -translate-y-1/2 animate-spin"
                style={{ color: 'var(--accent)' }}
              />
            )}
          </div>
        )}
      </div>

      {/* Content */}
      <div className="px-8 pb-8 flex-1">
        {loading ? (
          <LoadingState />
        ) : currentMods.length === 0 ? (
          <EmptyState tab={tab} hasQuery={!!searchQuery.trim()} />
        ) : (
          <div ref={gridRef} className="grid grid-cols-1 gap-3">
            {currentMods.map((mod, i) => (
              <ModCard
                key={mod.id ?? i}
                mod={mod}
                isInstalled={tab === 'installed' || installedMods.some(m => m.id === mod.id)}
                onInstall={() => { /* ipc.mods.install(mod.id) */ }}
                onUninstall={() => { /* ipc.mods.uninstall(mod.id) */ }}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

/* ─── Mod card ─── */

function ModCard({
  mod,
  isInstalled,
  onInstall,
  onUninstall,
}: {
  mod: ModItem;
  isInstalled: boolean;
  onInstall: () => void;
  onUninstall: () => void;
}) {
  const cardRef = useRef<HTMLDivElement>(null);

  return (
    <div ref={cardRef} className="mod-card">
      <GlassCard className="p-4 flex items-start gap-4 group" hover delay={0}>
        {/* Icon */}
        <div
          className="w-12 h-12 rounded-xl flex-shrink-0 flex items-center justify-center overflow-hidden"
          style={{ backgroundColor: 'var(--bg-lighter)' }}
        >
          {mod.iconUrl ? (
            <img src={mod.iconUrl} alt="" className="w-full h-full object-cover rounded-xl" />
          ) : (
            <Package size={20} style={{ color: 'var(--text-muted)' }} />
          )}
        </div>

        {/* Info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <h3 className="text-sm font-semibold truncate" style={{ color: 'var(--text-primary)' }}>
              {mod.name}
            </h3>
            {mod.featured && (
              <Star size={12} className="flex-shrink-0" style={{ color: 'var(--warning)', fill: 'var(--warning)' }} />
            )}
          </div>

          {mod.description && (
            <p className="text-xs line-clamp-2 mb-2" style={{ color: 'var(--text-secondary)' }}>
              {mod.description}
            </p>
          )}

          <div className="flex items-center gap-4">
            {mod.author && (
              <span className="text-[11px]" style={{ color: 'var(--text-muted)' }}>
                by {mod.author}
              </span>
            )}
            {mod.downloads != null && (
              <span className="flex items-center gap-1 text-[11px]" style={{ color: 'var(--text-muted)' }}>
                <Download size={10} />
                {formatCount(mod.downloads)}
              </span>
            )}
            {mod.version && (
              <span className="text-[10px] px-1.5 py-0.5 rounded" style={{ backgroundColor: 'var(--bg-lighter)', color: 'var(--text-muted)' }}>
                v{mod.version}
              </span>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className="flex items-center gap-2 flex-shrink-0">
          {isInstalled ? (
            <button
              onClick={onUninstall}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition-all duration-200 hover:scale-105"
              style={{ backgroundColor: 'rgba(248,113,113,0.1)', color: 'var(--error)', border: '1px solid rgba(248,113,113,0.2)' }}
            >
              <Trash2 size={13} />
              Remove
            </button>
          ) : (
            <button
              onClick={onInstall}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition-all duration-200 hover:scale-105"
              style={{
                background: 'linear-gradient(135deg, var(--accent), #6A4AE8)',
                color: 'white',
                boxShadow: '0 2px 8px var(--accent-glow)',
              }}
            >
              <Download size={13} />
              Install
            </button>
          )}
        </div>
      </GlassCard>
    </div>
  );
}

/* ─── States ─── */

function LoadingState() {
  return (
    <div className="flex items-center justify-center h-48">
      <div className="flex flex-col items-center gap-3">
        <RefreshCw size={28} className="animate-spin" style={{ color: 'var(--accent)' }} />
        <span className="text-sm" style={{ color: 'var(--text-muted)' }}>Loading mods...</span>
      </div>
    </div>
  );
}

function EmptyState({ tab, hasQuery }: { tab: Tab; hasQuery: boolean }) {
  return (
    <div className="flex items-center justify-center h-48">
      <div className="flex flex-col items-center gap-3">
        {tab === 'browse' && hasQuery ? (
          <>
            <AlertTriangle size={28} style={{ color: 'var(--warning)' }} />
            <span className="text-sm" style={{ color: 'var(--text-muted)' }}>No mods found</span>
          </>
        ) : tab === 'browse' ? (
          <>
            <Search size={28} style={{ color: 'var(--text-muted)' }} />
            <span className="text-sm" style={{ color: 'var(--text-muted)' }}>Type to search for mods</span>
          </>
        ) : (
          <>
            <Package size={28} style={{ color: 'var(--text-muted)' }} />
            <span className="text-sm" style={{ color: 'var(--text-muted)' }}>No mods installed yet</span>
            <span className="text-xs" style={{ color: 'var(--text-muted)' }}>Browse the catalog to get started</span>
          </>
        )}
      </div>
    </div>
  );
}

/* ─── Helpers ─── */

function formatCount(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return n.toString();
}
