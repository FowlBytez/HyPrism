import requests
from pathlib import Path
import sys

OWNER = "yyyumeniku"
REPO = "HyPrism"

install_dir = (
    Path(sys.argv[1]).expanduser()
    if len(sys.argv) > 1
    else Path.home() / "Applications" / "HyPrism"
)
install_dir.mkdir(parents=True, exist_ok=True)

api_url = f"https://api.github.com/repos/{OWNER}/{REPO}/releases/latest"
headers = {"User-Agent": "HyPrism-installer"}

resp = requests.get(api_url, headers=headers)
resp.raise_for_status()
release = resp.json()

asset_url = None
for asset in release["assets"]:
    name = asset["name"]
    if name.endswith(".AppImage") and "x86_64" in name:
        asset_url = asset["browser_download_url"]
        break

if not asset_url:
    raise RuntimeError("AppImage not found in latest release")

filename = install_dir / asset_url.split("/")[-1]

print(f"Downloading HyPrism to: {filename}")

with requests.get(asset_url, stream=True, headers=headers) as r:
    r.raise_for_status()
    with open(filename, "wb") as f:
        for chunk in r.iter_content(8192):
            if chunk:
                f.write(chunk)

filename.chmod(0o755)
print("HyPrism installed successfully!")

def ins_shortuc(app_path, install_dir):
    desktop = Path.home() / "Desktop"
    desktop.mkdir(exist_ok=True)
    shortcut_desktop = desktop / "HyPrism.desktop"

    shortcut_local_dir = Path.home() / ".local" / "share" / "applications"
    shortcut_local_dir.mkdir(parents=True, exist_ok=True)
    shortcut_local = shortcut_local_dir / "HyPrism.desktop"

    icon_src = Path(__file__).parent / "HyPrism_icon.png"
    icon_dst = install_dir / "HyPrism_icon.png"

    if icon_src.exists() and not icon_dst.exists():
        icon_dst.write_bytes(icon_src.read_bytes())

    desktop_entry = f"""[Desktop Entry]
Name=HyPrism
Comment=Hytale Launcher
Exec={app_path}
TryExec={app_path}
Icon={icon_dst}
Terminal=false
Type=Application
Categories=Game;
StartupWMClass=HyPrism
"""
    for shortcut in (shortcut_desktop, shortcut_local):
        shortcut.write_text(desktop_entry)
        shortcut.chmod(0o755)

    print(f"Shortcut created on desktop: {shortcut_desktop}")
    print(f"Shortcut added to menu: {shortcut_local}")

y = input("Do you want me to create a shortcut? [y/N] ").lower()
if y in ("y", "yes"):
    ins_shortuc(filename, install_dir)
else:
    print("Ok, goodbye")
