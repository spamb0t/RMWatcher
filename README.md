# RMWatcher

## Description

A simple app written in C# for periodically checking for updates on Reddit pages with magnet and/or torrent links (where RSS will not work). If changes to the post are detected, it automatically opens the magnet (or .torrent) in your associated app.

**Planned features**:
- Support for qBittorrent's Web API
- Linux support
- Support for other sites down the line using manual JSON.

### Requirements
- Windows 10 version 1809 (October 2018 update, build 17763) or later.
- No .NET installation required — all dependencies are included with the download.

### Installing & Launching the app

1. Just run the installer - ``RMW_setup_winx64.exe`` or ``RMW_setup_winx86.exe`` depending on what version you want.
2. Choose where to install it the app.
3. Click "Install".

### Executing program

Launch the app using ``RMWatcher.exe``.

Arguments:
- Using the ``--minimized`` argument will launch the app minimized to tray.

*(Note that the app will automatically start minimized when "Auto-run at boot" is enabled.)*

## Authors

Ji Svartklint (spamb0t)

## Version History

* 0.2-alpha
    * Initial release

## License

This project is licensed under the CC BY-NC 4.0 License.
[https://creativecommons.org/licenses/by-nc/4.0/](https://creativecommons.org/licenses/by-nc/4.0/)

This means you are free to share and modify the code as long as it's not for commercial use.
