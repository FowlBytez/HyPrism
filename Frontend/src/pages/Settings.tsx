import { useState, useEffect, useRef } from 'react';
import {
  Settings2, Palette, Gamepad2, Wrench,
  Globe, FolderOpen, Volume2, Monitor, Shield,
  ChevronDown, Check, RotateCcw,
} from 'lucide-react';
import gsap from 'gsap';
import { ipc } from '../lib/ipc';
import type { SettingsSnapshot } from '../lib/ipc';

interface Section {
  id: string;
  title: string;
  icon: React.ElementType;
  description: string;
}

const SECTIONS: Section[] = [
  { id: 'general',    title: 'General',    icon: Settings2, description: 'Language, paths, and startup preferences' },
  { id: 'appearance', title: 'Appearance', icon: Palette,   description: 'Theme, accent color, and visual effects' },
  { id: 'game',       title: 'Game',       icon: Gamepad2,  description: 'Launch options, memory, and Java settings' },
  { id: 'advanced',   title: 'Advanced',   icon: Wrench,    description: 'Developer options and diagnostics' },
];

export function Settings() {
  const [settings, setSettings] = useState<SettingsSnapshot | null>(null);
  const [openSection, setOpenSection] = useState<string>('general');
  const [saving, setSaving] = useState(false);
  const headerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    loadSettings();
  }, []);

  useEffect(() => {
    if (!headerRef.current) return;
    gsap.from(headerRef.current, { opacity: 0, y: -16, duration: 0.5, ease: 'power3.out' });
  }, []);

  const loadSettings = async () => {
    try {
      const data = await ipc.settings.get();
      setSettings(data);
    } catch {
      /* fallback */
    }
  };

  const updateField = async (key: string, value: unknown) => {
    if (!settings) return;
    const updated = { ...settings, [key]: value };
    setSettings(updated);
    setSaving(true);
    try {
      await ipc.settings.update(updated);
    } finally {
      setTimeout(() => setSaving(false), 600);
    }
  };

  const toggleSection = (id: string) => {
    setOpenSection(prev => prev === id ? '' : id);
  };

  return (
    <div className="flex flex-col h-full overflow-y-auto custom-scrollbar">
      {/* Header */}
      <div ref={headerRef} className="px-8 pt-7 pb-6 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div
            className="w-10 h-10 rounded-xl flex items-center justify-center"
            style={{ background: 'linear-gradient(135deg, var(--accent), #a78bfa)', boxShadow: '0 4px 15px var(--accent-glow)' }}
          >
            <Settings2 size={20} color="white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold" style={{ color: 'var(--text-primary)' }}>Settings</h1>
            <p className="text-xs" style={{ color: 'var(--text-muted)' }}>
              Customize your experience
            </p>
          </div>
        </div>

        {saving && (
          <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs"
            style={{ backgroundColor: 'rgba(74,222,128,0.1)', color: 'var(--success)' }}>
            <Check size={14} />
            Saved
          </div>
        )}
      </div>

      {/* Sections */}
      <div className="px-8 pb-8 space-y-3">
        {SECTIONS.map((section, i) => (
          <SettingsSection
            key={section.id}
            section={section}
            isOpen={openSection === section.id}
            onToggle={() => toggleSection(section.id)}
            settings={settings}
            onUpdate={updateField}
            delay={i * 0.06}
          />
        ))}

        {/* Reset footer */}
        <div className="pt-4 flex justify-end">
          <button
            onClick={loadSettings}
            className="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-medium transition-all duration-200 hover:scale-105"
            style={{ backgroundColor: 'rgba(248,113,113,0.1)', color: 'var(--error)', border: '1px solid rgba(248,113,113,0.2)' }}
          >
            <RotateCcw size={14} />
            Reset to defaults
          </button>
        </div>
      </div>
    </div>
  );
}

/* ─── Section accordion ─── */

function SettingsSection({
  section,
  isOpen,
  onToggle,
  settings,
  onUpdate,
  delay,
}: {
  section: Section;
  isOpen: boolean;
  onToggle: () => void;
  settings: SettingsSnapshot | null;
  onUpdate: (key: string, value: unknown) => void;
  delay: number;
}) {
  const contentRef = useRef<HTMLDivElement>(null);
  const wrapperRef = useRef<HTMLDivElement>(null);

  // Accordion GSAP animation
  useEffect(() => {
    if (!contentRef.current) return;
    if (isOpen) {
      gsap.to(contentRef.current, {
        height: 'auto',
        opacity: 1,
        duration: 0.35,
        ease: 'power2.out',
      });
    } else {
      gsap.to(contentRef.current, {
        height: 0,
        opacity: 0,
        duration: 0.25,
        ease: 'power2.in',
      });
    }
  }, [isOpen]);

  // Stagger-in on mount
  useEffect(() => {
    if (!wrapperRef.current) return;
    gsap.from(wrapperRef.current, {
      opacity: 0,
      y: 20,
      duration: 0.45,
      ease: 'power3.out',
      delay,
    });
  }, [delay]);

  const Icon = section.icon;

  return (
    <div ref={wrapperRef} className="section-card">
      {/* Header */}
      <button onClick={onToggle} className="w-full flex items-center justify-between p-4 group">
        <div className="flex items-center gap-3">
          <div
            className="w-8 h-8 rounded-lg flex items-center justify-center transition-colors"
            style={{
              backgroundColor: isOpen ? 'rgba(124,92,252,0.15)' : 'var(--bg-lighter)',
              color: isOpen ? 'var(--accent)' : 'var(--text-secondary)',
            }}
          >
            <Icon size={16} />
          </div>
          <div className="text-left">
            <div className="text-sm font-semibold" style={{ color: 'var(--text-primary)' }}>{section.title}</div>
            <div className="text-[11px]" style={{ color: 'var(--text-muted)' }}>{section.description}</div>
          </div>
        </div>
        <ChevronDown
          size={16}
          style={{
            color: 'var(--text-muted)',
            transform: isOpen ? 'rotate(180deg)' : 'rotate(0)',
            transition: 'transform 0.25s ease',
          }}
        />
      </button>

      {/* Content */}
      <div ref={contentRef} className="overflow-hidden" style={{ height: isOpen ? 'auto' : 0, opacity: isOpen ? 1 : 0 }}>
        <div className="px-4 pb-4 space-y-4">
          {section.id === 'general' && <GeneralSettings settings={settings} onUpdate={onUpdate} />}
          {section.id === 'appearance' && <AppearanceSettings settings={settings} onUpdate={onUpdate} />}
          {section.id === 'game' && <GameSettings settings={settings} onUpdate={onUpdate} />}
          {section.id === 'advanced' && <AdvancedSettings settings={settings} onUpdate={onUpdate} />}
        </div>
      </div>
    </div>
  );
}

/* ─── Setting row components ─── */

function SettingRow({ label, description, children }: { label: string; description?: string; children: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between py-2">
      <div className="flex-1 mr-4">
        <div className="text-sm" style={{ color: 'var(--text-primary)' }}>{label}</div>
        {description && <div className="text-[11px] mt-0.5" style={{ color: 'var(--text-muted)' }}>{description}</div>}
      </div>
      {children}
    </div>
  );
}

function ToggleSwitch({ checked, onChange }: { checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      className="toggle-switch"
      data-checked={checked || undefined}
      onClick={() => onChange(!checked)}
      role="switch"
      aria-checked={checked}
    >
      <span className="toggle-slider" />
    </button>
  );
}

function SelectInput({ value, options, onChange }: { value: string; options: { value: string; label: string }[]; onChange: (v: string) => void }) {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="px-3 py-1.5 rounded-lg text-xs outline-none cursor-pointer"
      style={{
        backgroundColor: 'var(--bg-lighter)',
        color: 'var(--text-primary)',
        border: '1px solid var(--glass-border)',
      }}
    >
      {options.map((opt) => (
        <option key={opt.value} value={opt.value}>{opt.label}</option>
      ))}
    </select>
  );
}

/* ─── Section content ─── */

function GeneralSettings({ settings, onUpdate }: { settings: SettingsSnapshot | null; onUpdate: (k: string, v: unknown) => void }) {
  return (
    <>
      <SettingRow label="Language" description="Interface language">
        <div className="flex items-center gap-2">
          <Globe size={14} style={{ color: 'var(--text-muted)' }} />
          <SelectInput
            value={settings?.language ?? 'en-US'}
            options={[
              { value: 'en-US', label: 'English' },
              { value: 'ru-RU', label: 'Русский' },
              { value: 'de-DE', label: 'Deutsch' },
              { value: 'es-ES', label: 'Español' },
              { value: 'fr-FR', label: 'Français' },
              { value: 'ja-JP', label: '日本語' },
              { value: 'ko-KR', label: '한국어' },
              { value: 'pt-BR', label: 'Português' },
              { value: 'tr-TR', label: 'Türkçe' },
              { value: 'uk-UA', label: 'Українська' },
              { value: 'zh-CN', label: '中文' },
              { value: 'be-BY', label: 'Беларуская' },
            ]}
            onChange={(v) => onUpdate('language', v)}
          />
        </div>
      </SettingRow>
      <SettingRow label="Game directory" description="Where game files are stored">
        <button
          onClick={() => { /* could open folder dialog via IPC */ }}
          className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs transition-colors"
          style={{ backgroundColor: 'var(--bg-lighter)', color: 'var(--text-secondary)', border: '1px solid var(--glass-border)' }}
        >
          <FolderOpen size={13} />
          Browse
        </button>
      </SettingRow>
      <SettingRow label="Launch on startup" description="Start HyPrism when system boots">
        <ToggleSwitch checked={settings?.launchOnStartup ?? false} onChange={(v) => onUpdate('launchOnStartup', v)} />
      </SettingRow>
      <SettingRow label="Minimize to tray" description="Keep running in system tray when closed">
        <ToggleSwitch checked={settings?.minimizeToTray ?? false} onChange={(v) => onUpdate('minimizeToTray', v)} />
      </SettingRow>
    </>
  );
}

function AppearanceSettings({ settings, onUpdate }: { settings: SettingsSnapshot | null; onUpdate: (k: string, v: unknown) => void }) {
  const ACCENTS = ['#7C5CFC', '#f43f5e', '#06b6d4', '#10b981', '#f59e0b', '#ec4899'];
  return (
    <>
      <SettingRow label="Accent color" description="Primary color for interactive elements">
        <div className="flex gap-2">
          {ACCENTS.map((color) => (
            <button
              key={color}
              onClick={() => onUpdate('accentColor', color)}
              className="w-6 h-6 rounded-full transition-transform hover:scale-110 relative"
              style={{
                backgroundColor: color,
                boxShadow: settings?.accentColor === color ? `0 0 0 2px var(--bg-darkest), 0 0 0 4px ${color}` : 'none',
              }}
            />
          ))}
        </div>
      </SettingRow>
      <SettingRow label="Animations" description="Enable motion design effects">
        <ToggleSwitch checked={settings?.animations !== false} onChange={(v) => onUpdate('animations', v)} />
      </SettingRow>
      <SettingRow label="Transparency" description="Window glassmorphism effects">
        <ToggleSwitch checked={settings?.transparency !== false} onChange={(v) => onUpdate('transparency', v)} />
      </SettingRow>
    </>
  );
}

function GameSettings({ settings, onUpdate }: { settings: SettingsSnapshot | null; onUpdate: (k: string, v: unknown) => void }) {
  return (
    <>
      <SettingRow label="Resolution" description="Game window resolution">
        <div className="flex items-center gap-2">
          <Monitor size={14} style={{ color: 'var(--text-muted)' }} />
          <SelectInput
            value={settings?.resolution ?? '1920x1080'}
            options={[
              { value: '1280x720', label: '1280×720' },
              { value: '1600x900', label: '1600×900' },
              { value: '1920x1080', label: '1920×1080' },
              { value: '2560x1440', label: '2560×1440' },
              { value: '3840x2160', label: '3840×2160' },
            ]}
            onChange={(v) => onUpdate('resolution', v)}
          />
        </div>
      </SettingRow>
      <SettingRow label="RAM allocation" description="Maximum memory for game (MB)">
        <input
          type="number"
          min={512}
          max={16384}
          step={256}
          value={settings?.ramMb as number ?? 4096}
          onChange={(e) => onUpdate('ramMb', parseInt(e.target.value) || 4096)}
          className="w-24 px-3 py-1.5 rounded-lg text-xs text-right outline-none"
          style={{ backgroundColor: 'var(--bg-lighter)', color: 'var(--text-primary)', border: '1px solid var(--glass-border)' }}
        />
      </SettingRow>
      <SettingRow label="Sound" description="Enable game audio">
        <div className="flex items-center gap-2">
          <Volume2 size={14} style={{ color: 'var(--text-muted)' }} />
          <ToggleSwitch checked={settings?.sound !== false} onChange={(v) => onUpdate('sound', v)} />
        </div>
      </SettingRow>
      <SettingRow label="Close launcher on game start" description="Free resources while playing">
        <ToggleSwitch checked={settings?.closeOnLaunch ?? false} onChange={(v) => onUpdate('closeOnLaunch', v)} />
      </SettingRow>
    </>
  );
}

function AdvancedSettings({ settings, onUpdate }: { settings: SettingsSnapshot | null; onUpdate: (k: string, v: unknown) => void }) {
  return (
    <>
      <SettingRow label="Developer mode" description="Show debug information and tools">
        <div className="flex items-center gap-2">
          <Shield size={14} style={{ color: 'var(--text-muted)' }} />
          <ToggleSwitch checked={settings?.developerMode ?? false} onChange={(v) => onUpdate('developerMode', v)} />
        </div>
      </SettingRow>
      <SettingRow label="Verbose logging" description="Extra detail in log files">
        <ToggleSwitch checked={settings?.verboseLogging ?? false} onChange={(v) => onUpdate('verboseLogging', v)} />
      </SettingRow>
      <SettingRow label="Allow pre-release" description="Receive beta and alpha updates">
        <ToggleSwitch checked={settings?.preRelease ?? false} onChange={(v) => onUpdate('preRelease', v)} />
      </SettingRow>
    </>
  );
}
