# Building HyPrism on Linux (Developer Guide) 🔧

This short guide explains common Linux build issues and how to get a working development environment for HyPrism.

## Prerequisites 💡

- **.NET runtime**: The app targets **.NET 8 (Microsoft.NETCore.App 8.0.x)**. You must have an appropriate .NET runtime available to run the compiled app. The system may also have a newer SDK (e.g., .NET 10) installed for building.
  - Check installed runtimes:
    - `dotnet --list-runtimes`
    - `dotnet --info`
  - Quick install (cross-distro using dotnet-install):
    ```bash
    curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
    bash dotnet-install.sh --channel 8.0 --runtime dotnet
    export PATH="$HOME/.dotnet:$PATH"
    export DOTNET_ROOT="$HOME/.dotnet"
    ```
  - On Fedora you may use:
    - `sudo dnf install dotnet-runtime-8.0`
  - After install verify `Microsoft.NETCore.App 8.0.x` appears in `dotnet --list-runtimes`.

  - Note: **runtime vs SDK** — Building the project requires the .NET **SDK** (not just a runtime). Check installed SDKs and runtimes:
    - `dotnet --list-sdks`
    - `dotnet --list-runtimes`

    If `No .NET SDKs were found`, install an SDK:
    - Fedora: `sudo dnf install dotnet-sdk-8.0`
    - Cross-distro (user-local install):
      ```bash
      curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
      bash dotnet-install.sh --channel 8.0 --install-dir $HOME/.dotnet
      export PATH="$HOME/.dotnet:$PATH"
      export DOTNET_ROOT="$HOME/.dotnet"
      ```
    After installation verify `dotnet --list-sdks` shows an 8.0 SDK.

    **PATH conflicts / Homebrew note:** If you have a Homebrew-provided `dotnet` (for example `/home/linuxbrew/...`) it may shadow the user-local `~/.dotnet` install. Check which `dotnet` is used with `which dotnet`. To prefer the user-local install for your shell session:
    ```bash
    export PATH="$HOME/.dotnet:$PATH"
    export DOTNET_ROOT="$HOME/.dotnet"
    ```
    Persist these lines in `~/.bashrc` / `~/.zshrc` if you want them applied for every shell.

- **Node.js & package manager** (for the frontend)
  - Recommended: Node 18+ (check `node -v`).
  - The frontend uses a lockfile (`package-lock.json`) and `vite` as a dev tool.
  - Install node packages in `frontend/` before building:
    ```bash
    cd frontend
    npm ci
    npm run build
    ```

## Local build flow ✅

1. Build frontend
   - `cd frontend && npm ci && npm run build`
2. Build backend
   - From repo root: `dotnet build`
3. Run (dev)
   - `dotnet run` (from repo root)

> The provided script `scripts/run.sh` will now attempt to detect missing frontend `node_modules` and install packages automatically using `npm ci`, `pnpm`, or `yarn` depending on the lockfile.

## Common failures & fixes ⚠️

- Error: `sh: 1: vite: command not found`
  - Cause: frontend dev deps are not installed so `vite` is not available.
  - Fix: `cd frontend && npm ci` (or run `./scripts/run.sh` which will auto-install deps if missing).

- Error: `You must install or update .NET to run this application. Framework 'Microsoft.NETCore.App', version '8.0.0'`
  - Cause: The runtime required by the app (8.0.x) is not present even if a newer SDK/runtime (e.g., .NET 10) is installed.
  - Fix (recommended): Install .NET 8 runtime (see instructions above).
  - Temporary workaround (not recommended for production):
    ```bash
    DOTNET_ROLL_FORWARD=Major dotnet bin/Debug/net8.0/HyPrism.dll
    ```
    This asks the host to try a later major runtime, which may work for local verification.

- Error: `Unable to load shared library 'Photino.Native' ... libwebkit2gtk-4.1.so.0: cannot open shared object file`
  - Cause: The native Photino library requires WebKitGTK and other system GUI libraries which are not installed on the system.
  - Fix: Install WebKitGTK (webkit2gtk-4.1) and GTK packages for your distribution. Examples:
    - Fedora:
      ```bash
      sudo dnf install webkit2gtk4.1
      ```
    - Debian / Ubuntu:
      ```bash
      sudo apt update && sudo apt install -y libwebkit2gtk-4.1-0 libgtk-3-0
      ```
    - Arch / Manjaro:
      ```bash
      sudo pacman -S webkit2gtk
      ```
      (package provides libwebkit2gtk-4.1)
  - After installing the native packages re-run `./scripts/run.sh`. See `scripts/run.sh` for an early runtime check that will warn when these libraries are missing.

## Tips for CI / Packaging 🧰

- Ensure CI installs both the required .NET runtime (or builds a self-contained publish) and runs `npm ci` in `frontend/` before building.
- For reproducible builds prefer lockfiles and `npm ci` / `pnpm install --frozen-lockfile` / `yarn install --frozen-lockfile`.

## Troubleshooting steps (quick) 🛠️

- Verify dotnet runtimes: `dotnet --list-runtimes`
- Verify Node and npm: `node -v && npm -v`
- Reinstall frontend deps: `cd frontend && npm ci`
- Run with roll-forward for quick test: `DOTNET_ROLL_FORWARD=Major dotnet bin/Debug/net8.0/HyPrism.dll`

---

If you'd like, I can also:
- Commit this file to `main` and open a PR with message `docs: add Linux build guide for developers` ✅
- Add a short entry to `README.md` linking to this guide 🔗

Would you like me to commit and create the PR now? 
