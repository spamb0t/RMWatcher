# RMWatcher

## Description

**RMWatcher** is a simple Windows utility (written in C#) for periodically checking for updates to Reddit posts that contain magnet and/or torrent links (useful where RSS won't work). If a post is updated, RMWatcher automatically opens the magnet or `.torrent` link with your associated app.

---

### Planned Features

- Support for qBittorrent's Web API
- Linux support
- Support for other sites via manual JSON

---

## Requirements

- **Windows 10 version 1809 (October 2018 update, build 17763) or later**
- **No .NET installation required — all dependencies are included with the download**

---

## Installing & Launching

1. Download and run either `RMW_setup_winx64.exe` or `RMW_setup_winx86.exe`, depending on your system.
2. Choose your preferred installation directory.
3. Optionally create desktop or Start Menu shortcuts during setup.
4. Click "Install".

---

## Usage

- You can launch RMWatcher using `RMWatcher.exe` from the installation folder.
- If you created shortcuts during install, use those for easier access.

**Command-line arguments:**
> `--minimized` &nbsp; Launches the app minimized to the system tray.  
> <sub>*(The app also starts minimized automatically if "Auto-run at boot" is enabled.)*</sub>

---

## Authors

- Ji Svartklint (spamb0t)

---

## Version History

- **0.2-alpha**  
    - Initial release

---

## License

This project is licensed under the [CC BY-NC 4.0 License](https://creativecommons.org/licenses/by-nc/4.0/).

> You are free to share and modify the code as long as it's not for commercial use.

---
