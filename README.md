# RMWatcher

## Description

**RMWatcher** is a Windows utility (written in C#) for monitoring Reddit posts (and soon any website) for updates containing magnet or `.torrent` links. If a change is detected, RMWatcher automatically opens the link in your default torrent application.

---

### Planned Features

- Support for qBittorrent's Web API
- Linux support
- Ability to monitor any website section:  
  Paste a URL and use a visual wizard to select which part of the page (element with a magnet/torrent link) you want to monitor—no technical knowledge required.

---

## Requirements

- **Windows 10 version 1809 (October 2018 update, build 17763) or later**
- **No .NET installation required** — all dependencies are included with the download

---

## Installing & Launching

1. Download the latest installer from the [Releases](../../releases) page.
2. Run the installer and choose your preferred installation directory.
3. Optionally create desktop or Start Menu shortcuts during setup.
4. Click "Install".

## Usage

- Launch RMWatcher from the folder you installed it to, or from any shortcut you created during installation.

**Command-line arguments:**
> `--minimized`  
> Launches the app minimized to the system tray.  
> <sub>*(The app also starts minimized automatically if "Auto-run at boot" is enabled.)*</sub>

---

## Authors

- Ji Svartklint (spamb0t)

---

## Version History

- **0.2.5-a.11**
  - URLs and their content saved (using a hash) between sessions.
  - Fixes to code to streamline workflow, making updates easier and faster.

- **0.2.5-a.8**
  - Rewrites to workflow and updated, better, DRY code.
  - Fixed "Minimize to Tray" behavior, added option for "Always launch minimized".

- **0.2.1-alpha**
	- Fixed crash on startup due to WPF Window / Tray icon conflict

- **0.2.0-alpha**  
    - Initial release

---

## Feedback & Bug Reports

Have a question, found a bug, or want to suggest a new feature (or even collaborate)?  
**Please use the [Issues](../../issues) tab on GitHub** to report problems or share ideas.  
You can also send in suggestions or bug reports to [suggestions@svartklint.se](mailto:suggestions@svartklint.se).  
All feedback is welcome!

---

## License

This project is licensed under the [CC BY-NC 4.0 License](https://creativecommons.org/licenses/by-nc/4.0/).

> You are free to share and modify the code as long as it's not for commercial use.

---
