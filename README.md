# Reddit Magnet Watcher

## Description

A simple app written in C# for periodically checking for updates to Reddit pages with magnet and/or torrent links (where RSS will not work). If changes to the post is detected, it automatically opens the magnet (or .torrent) in your associated app.

**Planned features**:
- Support for qBittorrent's Web API
- Linux support
- Support for other sites down the line using manual JSON .

### Dependencies

Windows 10 and above.

### Installing & Launching the app

1. Just run the installer - ``RMW_installer_winx64.exe`` or ``RMW_installer_winx86.exe`` depending on what version you want.
2. Choose where to install it the app.
3. Click "Install".

### Executing program

Launch the app using ``RMW.exe``.

Arguments:
``--minimized`` - will launch the app minimized to tray.

Note that the app will automatically start minimized when "Auto-run at boot" is enabled.

## Authors

Ji Svartklint (spamb0t)

## Version History

* 0.1-pre-alpha
    * Initial release

## License

This project is licensed under the CC BY-NC 4.0 License.
[https://creativecommons.org/licenses/by-nc/4.0/](https://creativecommons.org/licenses/by-nc/4.0/)

This means you are free to share and modify the code as long as it's not for commersial use.
