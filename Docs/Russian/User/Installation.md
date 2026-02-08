# Установка

## Требования

- **Windows 10/11** (x64), **Linux** (x64) или **macOS** (x64/arm64)
- Подключение к интернету для первого запуска (загрузка игры)

## Скачивание

Скачайте последний релиз со страницы [GitHub Releases](https://github.com/HyPrism/HyPrism/releases).

### Windows

1. Скачайте `HyPrism-win-x64.zip`
2. Распакуйте в любую папку
3. Запустите `HyPrism.exe`

### Linux

#### AppImage
1. Скачайте `HyPrism-linux-x64.AppImage`
2. Сделайте исполняемым: `chmod +x HyPrism-linux-x64.AppImage`
3. Запустите: `./HyPrism-linux-x64.AppImage`

#### Flatpak
```bash
flatpak install dev.hyprism.HyPrism
flatpak run dev.hyprism.HyPrism
```

### macOS

1. Скачайте `HyPrism-osx-x64.zip` (или `osx-arm64` для Apple Silicon)
2. Распакуйте и переместите `HyPrism.app` в папку «Программы»
3. Запустите из папки «Программы»

## Директория данных

HyPrism хранит свои данные (конфигурацию, файлы игры, логи) по следующим путям:

| ОС | Путь |
|----|------|
| Windows | `%APPDATA%/HyPrism/` |
| Linux | `~/.config/HyPrism/` |
| macOS | `~/Library/Application Support/HyPrism/` |

## Первый запуск

При первом запуске HyPrism:
1. Создаст директорию данных
2. Покажет главную панель управления
3. Загрузит файлы игры при нажатии на кнопку «Играть»
