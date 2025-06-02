# RMWatcher

## Description

**RMWatcher** is a Windows utility (written in C#) for monitoring Reddit posts (and soon any website) for updates containing magnet or `.torrent` links. If a change is detected, RMWatcher automatically opens the link in your default torrent application.

---

### How It Works

**RMWatcher** is a tool for tracking posts that are updated over time (such as megathreads or ongoing event posts), letting you automate downloads without constant manual refreshing.

- **Current Focus:**  
  **RMWatcher** only scans the *main body* of the specified post for changes or new links. It does not scan comments, subreddit feeds, or user pages.  
  (Monitoring comments is *intentionally* excluded for now, as anyone can post in the comments — including potentially unsafe links. This keeps things secure and predictable.)

- **Continuous Development:**  
  The app is in active development, with new features and expanded capabilities being added regularly.  
  Monitoring additional page elements and safer handling of comments are planned, but will only be introduced with proper safeguards.

**Have a suggestion or need a new feature?**  
[Open an issue on GitHub](../../issues) or email [suggestions@svartklint.se](mailto:suggestions@svartklint.se) — feedback is always welcome!

---

### Planned Features

- Support for monitoring all uploads by a user.
- Support for qBittorrent's Web API which will allow for:
  - Complete automation of downloads without having to supress torrent dialogs in the client options.
  - A way to fetch metadata for torrents, making it easy for users to pick what to monitor in posts with multiple links.
- Multi-Platform support
- Ability to monitor any website using a visual wizard - no technical knowledge required.

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
