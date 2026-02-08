import { useRef, useEffect, useCallback } from 'react';
import { Home, Newspaper, Package, Settings } from 'lucide-react';
import { useLocation, useNavigate } from 'react-router-dom';
import gsap from 'gsap';

interface NavItem {
  icon: typeof Home;
  label: string;
  path: string;
}

const navItems: NavItem[] = [
  { icon: Home, label: 'Dashboard', path: '/' },
  { icon: Newspaper, label: 'News', path: '/news' },
  { icon: Package, label: 'Mods', path: '/mods' },
  { icon: Settings, label: 'Settings', path: '/settings' },
];

export function Sidebar() {
  const location = useLocation();
  const navigate = useNavigate();
  const indicatorRef = useRef<HTMLDivElement>(null);
  const buttonsRef = useRef<(HTMLButtonElement | null)[]>([]);

  const activeIndex = navItems.findIndex(
    (item) => item.path === location.pathname || (item.path === '/' && location.pathname === ''),
  );

  // Morphing indicator animation
  useEffect(() => {
    if (!indicatorRef.current || activeIndex < 0) return;
    const btn = buttonsRef.current[activeIndex];
    if (!btn) return;

    const y = btn.offsetTop + btn.offsetHeight / 2 - 20; // center the 40px indicator
    gsap.to(indicatorRef.current, {
      y,
      opacity: 1,
      duration: 0.45,
      ease: 'power3.out',
    });
  }, [activeIndex]);

  // Staggered entry on mount
  useEffect(() => {
    gsap.from(buttonsRef.current.filter(Boolean), {
      opacity: 0,
      x: -12,
      stagger: 0.06,
      duration: 0.5,
      ease: 'power3.out',
      delay: 0.2,
    });
  }, []);

  const setRef = useCallback((idx: number) => (el: HTMLButtonElement | null) => {
    buttonsRef.current[idx] = el;
  }, []);

  return (
    <nav
      className="flex flex-col w-[var(--sidebar-width)] items-center py-5 gap-1.5 relative shrink-0"
      style={{ backgroundColor: 'var(--bg-dark)', borderRight: '1px solid var(--glass-border)' }}
    >
      {/* Active indicator pill */}
      <div
        ref={indicatorRef}
        className="absolute left-0 w-[3px] h-10 rounded-r-full opacity-0"
        style={{ backgroundColor: 'var(--accent)', boxShadow: '0 0 12px var(--accent-glow)' }}
      />

      {navItems.map((item, idx) => {
        const isActive = idx === activeIndex;
        return (
          <button
            key={item.path}
            ref={setRef(idx)}
            onClick={() => navigate(item.path)}
            className="relative w-11 h-11 flex items-center justify-center rounded-xl transition-all duration-200 group"
            style={{
              backgroundColor: isActive ? 'var(--accent-subtle)' : 'transparent',
              color: isActive ? 'var(--accent)' : 'var(--text-muted)',
            }}
            title={item.label}
          >
            <item.icon size={20} className="transition-transform duration-200 group-hover:scale-110" />

            {/* Tooltip */}
            <div
              className="absolute left-full ml-3 px-2.5 py-1 rounded-lg text-xs font-medium whitespace-nowrap
                         opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity duration-200"
              style={{ backgroundColor: 'var(--bg-light)', color: 'var(--text-primary)', border: '1px solid var(--glass-border)' }}
            >
              {item.label}
            </div>
          </button>
        );
      })}
    </nav>
  );
}
