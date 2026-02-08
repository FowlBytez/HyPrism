# Installation

## Requirements

- **Windows 10/11** (x64), **Linux** (x64), or **macOS** (x64/arm64)
- Internet connection for first launch (game download)

## Download

Download the latest release from the [GitHub Releases](https://github.com/HyPrism/HyPrism/releases) page.

### Windows

1. Download `HyPrism-win-x64.zip`
2. Extract to any folder
3. Run `HyPrism.exe`

### Linux

#### AppImage
1. Download `HyPrism-linux-x64.AppImage`
2. Make executable: `chmod +x HyPrism-linux-x64.AppImage`
3. Run: `./HyPrism-linux-x64.AppImage`

#### Flatpak
```bash
flatpak install dev.hyprism.HyPrism
flatpak run dev.hyprism.HyPrism
```

### macOS

1. Download `HyPrism-osx-x64.zip` (or `osx-arm64` for Apple Silicon)
2. Extract and move `HyPrism.app` to Applications
3. Launch from Applications

## Data Directory

HyPrism stores its data (config, game files, logs) in:

| OS | Path |
|----|------|
| Windows | `%APPDATA%/HyPrism/` |
| Linux | `~/.config/HyPrism/` |
| macOS | `~/Library/Application Support/HyPrism/` |

## First Launch

On first launch, HyPrism will:
1. Create the data directory
2. Show the main dashboard
3. Download game files when you click "Play"
