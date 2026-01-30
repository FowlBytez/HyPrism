#!/bin/bash

# HyPrism Launcher Script
# This script builds and runs the HyPrism launcher

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

echo "Building HyPrism Launcher..."

# Build frontend
echo "Building frontend..."
FRONTEND_DIR="$REPO_ROOT/frontend"
cd "$FRONTEND_DIR"

# Install dependencies if needed
if [ ! -d "$FRONTEND_DIR/node_modules" ]; then
  echo "Installing frontend dependencies..."
  if [ -f "$FRONTEND_DIR/package-lock.json" ]; then
    npm ci
  elif [ -f "$FRONTEND_DIR/pnpm-lock.yaml" ]; then
    pnpm install
  elif [ -f "$FRONTEND_DIR/yarn.lock" ]; then
    yarn install --frozen-lockfile || yarn install
  else
    npm install
  fi
fi

npm run build
cd "$REPO_ROOT"

# Build and run C# backend
echo "Building backend..."
dotnet build

echo "Starting HyPrism..."

# Check for native dependencies required by Photino (libwebkit2gtk-4.1)
if ! ldconfig -p | grep -q "libwebkit2gtk-4.1"; then
  echo "WARNING: Required native library 'libwebkit2gtk-4.1' not found. Photino may fail to start."
  # Try to detect distro and suggest install commands
  if [ -f /etc/os-release ]; then
    . /etc/os-release
    case "$ID" in
      fedora)
        echo "On Fedora: sudo dnf install webkit2gtk4.1"
        ;;
      ubuntu|debian)
        echo "On Debian/Ubuntu: sudo apt update && sudo apt install -y libwebkit2gtk-4.1-0 libgtk-3-0"
        ;;
      arch)
        echo "On Arch: sudo pacman -S webkit2gtk"
        ;;
      *)
        echo "Install webkit2gtk-4.1 (package name may vary by distro)."
        ;;
    esac
  else
    echo "Install webkit2gtk-4.1 (package name may vary by distro)."
  fi
  echo "
See docs/BUILD-LINUX.md for more details. Exiting."
  exit 1
fi

# Run the app
dotnet run
