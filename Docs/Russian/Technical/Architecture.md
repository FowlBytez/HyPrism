# Архитектура

## Обзор

HyPrism использует архитектурный паттерн **Console + IPC + React SPA**:

```
┌─────────────────────────────────────────────────────┐
│  .NET Console App  (Program.cs)                     │
│  ├── Bootstrapper.cs (DI-контейнер)                 │
│  ├── Services/ (бизнес-логика)                      │
│  └── IpcService.cs (реестр IPC-каналов)             │
│         ↕ Сокетный мост Electron.NET                │
│  ┌─────────────────────────────────────────────┐    │
│  │  Electron Main Process                      │    │
│  │  └── BrowserWindow (безрамочное окно)       │    │
│  │       └── preload.js (contextBridge)        │    │
│  │            ↕ ipcRenderer                    │    │
│  │       ┌─────────────────────────────┐       │    │
│  │       │  React SPA                  │       │    │
│  │       │  ├── App.tsx (маршрутизация)│       │    │
│  │       │  ├── pages/ (страницы)      │       │    │
│  │       │  ├── components/ (общие)    │       │    │
│  │       │  └── lib/ipc.ts (генерир.)  │       │    │
│  │       └─────────────────────────────┘       │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

## Процесс запуска

1. `Program.Main()` инициализирует логгер Serilog
2. Устанавливает `ElectronLogInterceptor` на `Console.Out`/`Console.Error`
3. `Bootstrapper.Initialize()` собирает DI-контейнер
4. `ElectronNetRuntime.RuntimeController.Start()` порождает процесс Electron
5. `ElectronBootstrap()` создаёт безрамочное BrowserWindow, загружающее `file://wwwroot/index.html`
6. `IpcService.RegisterAll()` регистрирует все обработчики IPC-каналов
7. React SPA монтируется, загружает данные через типизированные IPC-вызовы

## Модель взаимодействия

Всё взаимодействие фронтенда и бэкенда использует **именованные IPC-каналы**:

```
Именование каналов: hyprism:{домен}:{действие}
Примеры:            hyprism:game:launch
                    hyprism:settings:get
                    hyprism:i18n:set
```

### Типы каналов

| Тип | Направление | Паттерн |
|-----|-------------|---------|
| **send** | React → .NET (без ожидания ответа) | `send(channel, data)` |
| **invoke** | React → .NET → React (запрос/ответ) | `invoke(channel, data)` → ожидает `:reply` |
| **event** | .NET → React (push-уведомление) | `on(channel, callback)` |

### Модель безопасности

- `contextIsolation: true` — рендерер не имеет доступа к Node.js
- `nodeIntegration: false` — нет `require()` в рендерере
- `preload.js` предоставляет только `window.electron.ipcRenderer` через contextBridge

## Внедрение зависимостей

Все сервисы регистрируются как синглтоны в `Bootstrapper.cs`.

## Перехват логов

Electron.NET выводит неструктурированные сообщения в stdout/stderr. HyPrism перехватывает их через `ElectronLogInterceptor` и перенаправляет в Logger:
- Сообщения фреймворка → Logger.Info
- Отладочные сообщения → Logger.Debug
- Паттерны ошибок → Logger.Warning
- Шумовые паттерны → подавляются
