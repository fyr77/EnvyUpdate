# EnvyUpdate
 A small update checker application for Nvidia GPUs
 
 ## How to use
 
 1. Download the latest release from the [releases page](https://github.com/fyr77/EnvyUpdate/releases) and run it. Windows SmartScreen Messages can be safely ignored. They only happen because this project is not digitally signed.
 2. If you want to use the application without saving any settings to your drive, keep the "Portable mode" checkbox checked. Otherwise uncheck it to automatically save your configuration.
 3. Install the cookie-txt addon for [Firefox](https://addons.mozilla.org/en-US/firefox/addon/cookies-txt-one-click/) or [Chrome](https://chrome.google.com/webstore/detail/cookiestxt/njabckikapfpffapmjgojcnbfjonfjfg).
 4. Go to the [Nvidia driver download page](https://www.nvidia.com/Download/index.aspx), enter your graphics card model, operating system, etc. and click "Search".
 5. On the resulting page, use the previously installed cookie-txt addon to save your site cookies as a .txt file.
 6. Drag this .txt file into the corresponding space inside EnvyUpdate.
 7. If everything works correctly, the online driver version should be displayed in the application window. 
 8. When not in portable mode, you may activate Autostart. This will make the application start everytime Windows boots. This will also install the application itself in your APPDATA folder.

## Licenses

This project: [MIT](https://github.com/fyr77/EnvyUpdate/blob/master/LICENSE)
Fody (for embedding DLLs into the main executable): [MIT](https://github.com/Fody/Fody/blob/master/License.txt)
wpf-notifyicon (for showing an icon in the system tray): [CPOL](https://github.com/hardcodet/wpf-notifyicon/blob/master/LICENSE)
