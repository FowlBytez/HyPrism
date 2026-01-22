# HyPrism Linux Installer

This document explains how to install and run HyPrism on Linux using the provided bash script.

## Features

* Downloads the latest `x86_64` AppImage from the official HyPrism GitHub releases.
* Makes the AppImage executable.
* Creates Desktop and application menu shortcuts (`.desktop` files) with proper icons.
* Automatically sets up a Python virtual environment and installs required dependencies (`requests`).

## Requirements

* Linux system with bash
* Python 3 installed
* pip installed

## Usage

1. Make sure the bash script is executable:

```bash
chmod +x install.sh
```

2. Run the installer:

```bash
./install.sh [installation_directory]
```

* `[installation_directory]` is optional. Default is `~/Applications/HyPrism`.

3. The script will:

   * Check for Python 3 and pip
   * Create a virtual environment (`.venv`) if it doesn't exist
   * Install dependencies
   * Run the Python installer (`main.py`) to download AppImage and create shortcuts

4. When prompted, type `y` to create shortcuts:

   * Desktop: `~/Desktop/HyPrism.desktop`
   * Application menu: `~/.local/share/applications/HyPrism.desktop`

## Notes

* Tested on Arch Linux. Should work on most Linux distributions with Python 3 and bash.
* If the icon does not appear in the menu, ensure `HyPrism_icon.png` is in the installation directory.
* You can re-run the installer at any time to update AppImage or recreate shortcuts.

