
# HyPrism Linux Installer

This document explains how to use the Linux installer script for HyPrism.

## Features

* Downloads the latest `x86_64` AppImage from the official HyPrism GitHub releases.
* Makes the AppImage executable.
* Creates shortcuts on the Desktop and in the application menu (`.desktop` files) with proper icons.

## Usage

1. Open a terminal and run the installer:

```bash
python3 main.py [installation_directory]
```

* `[installation_directory]` is optional. Default is `~/Applications/HyPrism`.

2. The script will download the latest AppImage and make it executable.

3. You will be asked if you want to create shortcuts. Type `y` to create them.

4. Shortcuts will be placed:

   * Desktop: `~/Desktop/HyPrism.desktop`
   * Application menu: `~/.local/share/applications/HyPrism.desktop`

## Requirements

* Python 3
* Linux system with XDG-compliant Desktop Environment (GNOME, KDE, etc.)

## Notes

* If the icon does not appear in the menu, make sure `HyPrism_icon.png` is in the installation directory.
* Tested on Arch Linux with AppImage support.

