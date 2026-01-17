# Linux Installation Guide

HyPrism on Linux is available as Flatpak, AppImage, and standalone binary.

## Recommended: Flatpak

Flatpak bundles all dependencies for maximum compatibility.

### Prerequisites

Install Flatpak if not already installed:

```bash
# Ubuntu/Debian
sudo apt install flatpak

# Fedora
sudo dnf install flatpak

# Arch Linux
sudo pacman -S flatpak
```

### Install HyPrism

1. Download `HyPrism.flatpak` from [releases](https://github.com/yyyumeniku/HyPrism/releases/latest)
2. Install: `flatpak install HyPrism.flatpak`
3. Run: `flatpak run dev.hyprism.HyPrism`

## Alternative: AppImage

### Prerequisites

Install WebKitGTK 4.0:

```bash
# Ubuntu/Debian
sudo apt install libwebkit2gtk-4.0-37

# Fedora
sudo dnf install webkit2gtk4.0

# Arch Linux
sudo pacman -S webkit2gtk-4.1
```

### Install & Run

1. Download `HyPrism-x86_64.AppImage` from [releases](https://github.com/yyyumeniku/HyPrism/releases/latest)
2. Make it executable: `chmod +x HyPrism-x86_64.AppImage`
3. Run: `./HyPrism-x86_64.AppImage`

## Alternative: Binary (tar.gz)

If AppImage doesn't work, use the standalone binary:

1. Download `HyPrism-linux-x86_64.tar.gz` from [releases](https://github.com/yyyumeniku/HyPrism/releases/latest)
2. Extract: `tar -xzf HyPrism-linux-x86_64.tar.gz`
3. Run: `./HyPrism`

## Troubleshooting

### "libwebkit2gtk-4.0.so.37: cannot open shared object file"

Your system is missing WebKitGTK. Use Flatpak (recommended) or install WebKitGTK using the commands above.

### AppImage won't launch

Try Flatpak instead, or extract and run directly:
```bash
./HyPrism-x86_64.AppImage --appimage-extract
./squashfs-root/AppRun
```

### Game launches but crashes

1. Update to the latest HyPrism release
2. Ensure you have the latest graphics drivers
3. Try using Flatpak for isolated environment

## SteamOS / Steam Deck

For Steam Deck, Flatpak is recommended:

```bash
flatpak install HyPrism.flatpak
flatpak run dev.hyprism.HyPrism
```

Or use AppImage in Desktop Mode:
```bash
chmod +x HyPrism-x86_64.AppImage
./HyPrism-x86_64.AppImage
```

## Building from Source

See [CONTRIBUTING.md](CONTRIBUTING.md) for build instructions.

## Support

Report issues at [GitHub Issues](https://github.com/yyyumeniku/HyPrism/issues)
