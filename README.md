![Logo](https://github.com/fyr77/envyupdate/blob/master/res/banner_bg.png?raw=true)

# Important Information

Nvidia has discontinued non-DCH drivers. This means only Windows 10 and Windows 11 can get the most recent driver versions. 

If you are running Standard (non-DCH) drivers right now, EnvyUpdate will **NOT** display newer driver versions until you have manually updated to DCH drivers.

# EnvyUpdate
 A small portable update checker application for Nvidia GPUs
 
 ![License](https://img.shields.io/github/license/fyr77/envyupdate?style=for-the-badge)
 ![Issues](https://img.shields.io/github/issues/fyr77/envyupdate?style=for-the-badge)
 ![Version](https://img.shields.io/github/v/release/fyr77/envyupdate?style=for-the-badge)
 
## How to use

Download the [latest release](https://github.com/fyr77/EnvyUpdate/releases/latest/download/EnvyUpdate.exe) (or as a [.zip](https://github.com/fyr77/EnvyUpdate/releases/latest/download/EnvyUpdate.zip)) and run it. Windows SmartScreen Messages can be safely ignored. They only happen because this project is not digitally signed.

The application itself does not update itself. If you notice any bugs or issues, be sure to check for a new version on GitHub!

Enabling Autostart will create a shortcut of EnvyUpdate in the Windows startup folder.

### Virus warnings

Sometimes EnvyUpdate is flagged as a virus by Windows Defender or other antivirus software. This is a **false positive**, caused by EnvyUpdate reading a few values from the Windows registry and looking for files.

For extracting the driver installer 7-Zip is downloaded if it cannot be found on the user's system. This may also trigger antivirus warnings, but is necessary for the automatic driver installation to work.

## Compatibility

The application should be compatible with all Nvidia GeForce GPUs that have their drivers available on the nvidia.com download page and runs on Windows 10 and up.

It is tested with GeForce Series GPUs. Generally others might work, but they are (currently) untested.

## Development

This application is currently maintained and developed by me (fyr77) alone in my free time. 

I always try to implement critical fixes as fast as I can, but other features and minor bug fixes may take a few days or weeks to implement. 

If you want to help me develop EnvyUpdate, you can start by creating issues with your bug reports and/or feature requests. Pull requests are also welcome, especially regarding translations.

## Other interesting tools

* [TinyNvidiaUpdateChecker](https://github.com/ElPumpo/TinyNvidiaUpdateChecker) - a command line update checker and installer. Inspired EnvyUpdate to begin with.
* [nvidia-update](https://github.com/ZenitH-AT/nvidia-update) - a Powershell script to check for driver updates
* [Disable-Nvidia-Telemetry](https://github.com/NateShoffner/Disable-Nvidia-Telemetry) - does pretty much what the name says. It disables Nvidia Telemetry.
* [NVCleanInstall](https://www.techpowerup.com/nvcleanstall/) - a closed-source application by TechPowerUp which does quite a lot of cool things.

EnvyUpdate is not a replacement for any of these tools. I will still try to implement as many features in EnvyUpdate as possible while keeping the simple interface and as little settings as possible.

## Licenses

* This project: [MIT](https://github.com/fyr77/EnvyUpdate/blob/master/LICENSE)
* Fody (dependency of Costura.Fody): [MIT](https://github.com/Fody/Fody/blob/master/License.txt)
* Costura.Fody (for embedding DLLs into the main executable): [MIT](https://github.com/Fody/Costura/blob/develop/LICENSE)
* Resource Embedder: [MIT](https://www.nuget.org/packages/Resource.Embedder/)
* Windows Community Toolkit: [MIT](https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/main/License.md)
* wpfui: [MIT](https://github.com/lepoco/wpfui/blob/main/LICENSE)
* 7-Zip R: [GNU LGPL](https://www.7-zip.org/license.txt)
* Icon made by Freepik from www.flaticon.com
