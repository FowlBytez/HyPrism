# Введение

**HyPrism** — кроссплатформенный лаунчер игры Hytale, построенный на современных технологиях.

## Технологический стек

| Уровень | Технология |
|---------|-----------|
| Бэкенд | .NET 10, C# 13 |
| Оболочка | Electron.NET (ElectronNET.Core 0.4.0, Electron 34) |
| Фронтенд | React 19 + TypeScript 5.9 + Vite 7 |
| Анимации | GSAP 3 + @gsap/react |
| Стилизация | TailwindCSS v4 |
| Иконки | Lucide React |
| Маршрутизация | React Router DOM |
| Внедрение зависимостей | Microsoft.Extensions.DependencyInjection |
| Логирование | Serilog |

## Принцип работы

HyPrism запускается как **консольное приложение .NET**, которое порождает процесс **Electron**. Окно Electron загружает React SPA из локальной файловой системы. Всё взаимодействие между React-фронтендом и .NET-бэкендом происходит через **IPC-каналы** (Inter-Process Communication — межпроцессное взаимодействие).

```
.NET Console App → порождает процесс Electron
  ├── Electron Main Process
  │     └── BrowserWindow (безрамочное окно, contextIsolation)
  │           └── preload.js (contextBridge → ipcRenderer)
  └── React SPA (загружается из file://wwwroot/index.html)
        └── ipc.ts → IPC-каналы → IpcService.cs → .NET-сервисы
```

Это **НЕ** веб-сервер — здесь нет ASP.NET, нет HTTP, нет REST. Фронтенд взаимодействует с бэкендом исключительно через именованные IPC-каналы по сокетному мосту Electron.

## Ключевые принципы

1. **Единый источник истины** — C#-аннотации в `IpcService.cs` определяют все IPC-каналы и TypeScript-типы; IPC-клиент фронтенда на 100% генерируется автоматически
2. **Изоляция контекста** — `contextIsolation: true`, `nodeIntegration: false`; все API Electron доступны через `preload.js`
3. **Повсеместное внедрение зависимостей** — Все .NET-сервисы регистрируются в `Bootstrapper.cs` через конструкторное внедрение
4. **Кроссплатформенность** — Поддержка Windows, Linux, macOS через .NET 10 + Electron

## Поддерживаемые платформы

- **Windows** 10/11 (x64)
- **Linux** (x64) — AppImage, Flatpak
- **macOS** (x64, arm64)
