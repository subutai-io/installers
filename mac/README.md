# Subutai Mac OS X Installer

This document shows how to build a .pkg installer for Mac OS X

##Prerequisites:
* [VirtualBox >5.0] (https://www.virtualbox.org/wiki/Downloads)
* [Preconfigured Ubuntu Snappy OVA image] (https://www.dropbox.com/s/jj28sgj9xlg3zew/snappy.ova?dl=0)
* [Subutai snap package for Resource host] (https://github.com/subutai-io/Subutai-snappy)
* [Google Chrome with Subutai PGP plugin installed] (https://github.com/subutai-io/Tooling-pgp-plugin)
* [SubutaiTray app] (https://github.com/subutai-io/SubutaiTray)
* [P2P] (https://github.com/subutai-io/p2p)
* [TUN/TAP drivers] (http://tuntaposx.sourceforge.net/download.xhtml)
* Management and master template for Subutai
* Installation scripts

##Build directory
Create a folder with name subutai in your home directory and download all prerequisites there. Create these folders in subutai directory and place components accordingly:
```
scripts - for installation scripts
dist - for VirtualBox and TUN/TAP driver
templates - for Subutai management and master templates
snap - for Subutai snap package
snappy.ova image, Google Chrome, p2p and SubutaiTray app must be placed under the root of subutai directory.
```
So it should look similar to this:
```
├── Google Chrome.app
├── SubutaiTray.app
├── dist
│   └── VirtualBox.pkg
│   └── tuntap_20150118.pkg
├── p2p
├── scripts
│   ├── postinstall
│   ├── preinstall
│   └── prepare-server
├── snap
│   └── subutai_4.0.0_amd64.snap
├── snappy.ova
└── templates
    ├── management-subutai-template_4.0.0_amd64.tar.gz
    └── master-subutai-template_4.0.0_amd64.tar.gz
```

##Build

Run these commands from terminal:
```bash
pkgbuild --analyze --root ~/subutai components.plist
pkgbuild --identifier io.subutai.pkg.Subutai-installer --root ~/subutai --component-plist components.plist --scripts ~/subutai/scripts/ --install-location /Applications/Subutai/ ~/Documents/Subutai.pkg
```
This will build a package named Subutai.pkg and place it in ~/Documents/
Run the package and install it. Installer will run preinstallation script that will check whether VirtualBox is installed on the system, and if it's not, it will install it. All components will be placed under /Applications/Subutai directory. After that it will run postinstallation script that will import the OVA image to VirtualBox, install Subutai on it and then start it. Finnaly you will see "Installaion success" message. 

##Run

To access the Subutai console, open the Chrome browser with PGP plugin from /Applications/Subutai, generate your PGP key and go to https://localhost:9999
