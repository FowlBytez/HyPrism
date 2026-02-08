# Configuration

HyPrism stores its configuration in `config.json` inside the data directory.

## Settings

Access settings through the **Settings** page (gear icon in sidebar).

### General

| Setting | Description | Default |
|---------|-------------|---------|
| Language | UI language | System language or en-US |
| Close after launch | Close launcher when game starts | false |
| Launch on startup | Auto-start with OS | false |
| Minimize to tray | Minimize to system tray | false |

### Appearance

| Setting | Description | Default |
|---------|-------------|---------|
| Accent color | Theme accent color | Purple (#7C5CFC) |
| Animations | Enable UI animations | true |
| Transparency | Glass-morphism effects | true |
| Background mode | Dashboard background style | default |

### Game

| Setting | Description | Default |
|---------|-------------|---------|
| Resolution | Game window resolution | 1920x1080 |
| RAM allocation | Memory for game (MB) | 4096 |
| Sound | Game sound enabled | true |

### Advanced

| Setting | Description | Default |
|---------|-------------|---------|
| Developer mode | Show developer tools | false |
| Verbose logging | Extended log output | false |
| Pre-release | Receive pre-release updates | false |
| Launcher branch | Release or pre-release channel | release |
| Data directory | Custom data storage path | Platform default |

## Configuration File

**Location:**
- Windows: `%APPDATA%/HyPrism/config.json`
- Linux: `~/.config/HyPrism/config.json`
- macOS: `~/Library/Application Support/HyPrism/config.json`

The config file is JSON and can be edited manually, but it's recommended to use the Settings page.

## Profiles

HyPrism supports multiple player profiles. Switch between profiles via the sidebar profile selector. Each profile stores:
- Player nickname
- UUID
- Avatar (optional)
