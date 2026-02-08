# Сборка

## Предварительные требования

- **.NET 10 SDK**
- **Node.js 20+** (включает npm)
- **Git**

## Разработка

### Полная сборка (бэкенд + фронтенд)

```bash
dotnet build
```

Эта единственная команда запускает весь конвейер MSBuild:

1. `NpmInstall` — запускает `npm ci` в `Frontend/`
2. `GenerateIpcTs` — генерирует `Frontend/src/lib/ipc.ts` из C#-аннотаций
3. `BuildFrontend` — запускает `npm run build` (TypeScript + Vite)
4. `CopyFrontendDist` — копирует `Frontend/dist/` → `bin/.../wwwroot/`
5. Стандартная компиляция .NET

### Запуск

```bash
dotnet run
```

Запускает консольное приложение .NET → порождает Electron → открывает окно.

### Разработка только фронтенда

```bash
cd Frontend
npm run dev    # Vite dev server на localhost:5173
```

Удобно для итерации над UI без перезапуска всего приложения. Примечание: IPC-вызовы не будут работать в автономном режиме (нет моста Electron).

### Перегенерация IPC

```bash
node Scripts/generate-ipc.mjs
```

Или запускается автоматически через `dotnet build` при изменении `IpcService.cs`.

## Продакшн-сборка

```bash
# Сборка фронтенда для продакшна
cd Frontend && npm run build

# Публикация .NET
dotnet publish -c Release
```

Результат публикации находится в `bin/Release/net10.0/linux-x64/publish/` (или эквивалент для вашей платформы) и включает папку `wwwroot/` со скомпилированным фронтендом.

## Особенности платформ

### Linux

```bash
# Стандартная сборка
dotnet build

# Продакшн-публикация
dotnet publish -c Release -r linux-x64

# Flatpak (см. Packaging/flatpak/)
flatpak-builder build Packaging/flatpak/dev.hyprism.HyPrism.json
```

### macOS

```bash
dotnet publish -c Release -r osx-x64
# Или для Apple Silicon:
dotnet publish -c Release -r osx-arm64
```

См. `Packaging/macos/Info.plist` для macOS-специфичных метаданных.

### Windows

```bash
dotnet publish -c Release -r win-x64
```

## Цели MSBuild

| Цель | Триггер | Назначение |
|------|---------|-----------|
| `NpmInstall` | Перед `GenerateIpcTs` | `npm ci --prefer-offline` |
| `GenerateIpcTs` | Перед `BuildFrontend` | `node Scripts/generate-ipc.mjs` |
| `BuildFrontend` | Перед `Build` | `npm run build` в Frontend/ |
| `CopyFrontendDist` | После `Build` | Копирование dist → wwwroot |

Все цели используют инкрементальную сборку (Inputs/Outputs) для избежания лишней работы.
