# Features

## Core

- **Game launching** — Download, install, patch, and launch Hytale
- **Multi-instance** — Manage multiple game installations
- **Auto-updates** — Launcher self-update via GitHub releases
- **Profile management** — Multiple player profiles with avatar support

## User Interface

- **Modern dark UI** — Custom frameless window with glass-morphism design
- **GSAP animations** — Smooth page transitions and micro-interactions
- **Responsive layout** — Sidebar navigation, dashboard, news feed, settings, mod manager
- **Theme system** — CSS custom properties for full color customization

## Modding

- **Mod browser** — Search and discover mods (CurseForge integration)
- **Installed mods** — View and manage installed modifications
- **Mod metadata** — Version, author, description, download count

## Social

- **Hytale news** — Latest news feed from official sources
- **Discord integration** — Rich Presence status while playing

## Internationalization

- **12 languages** — en-US, ru-RU, de-DE, es-ES, fr-FR, ja-JP, ko-KR, pt-BR, tr-TR, uk-UA, zh-CN, be-BY
- **Runtime switching** — Change language without restart
- **Nested keys** — Structured localization with placeholder support (`{0}`, `{1}`)

## Developer

- **IPC code generation** — Single-source C# annotations generate typed TypeScript IPC client
- **MSBuild pipeline** — Automated `npm install → IPC codegen → Vite build → copy dist` on `dotnet build`
- **Serilog logging** — Structured file logging with console output and Electron.NET log interception
- **Flatpak packaging** — Linux packaging with manifest
