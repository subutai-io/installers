# Subutai Windows Installer
Subutai installation consists of two parts:
1. Installer, created using Visual Studio Installation Project extention, performs mimimal job: copies files, needed for further installation, on target machine, defines installation directory, creates \Software\Subutai Social\Subutai subkeys in HKEY_CURRENT_USER and HKEY_LOCAL_MACHINE hives, and runs executable.
2. Binary, created with Visual Studio 2015 project, performs the main stage of installation process:
<ul>
	<li> Change environment variabled %Path% and %Subutai%</li>
	<li> Download files needed for further installation</li>
	<li> Verify MD5 checksums </li>
	<li> Install software needed</li>
	<li> Setup virtual machine </li>
	<li> Setup virtual machine network</li>
	<li> Install/configure/start Subutai Social P2P service </li>
	<li> Create shortcuts </li>
</ul>

# How to install
Run installer w/Administrative privileges on fresh Windows 7-eng/8/10 x64 machine </br>
Wait until installation process finish install </br>
After installation finished You will see SubutaiTray login form, log in with Your Hub account, SubutaiTray icon will appear in Windows tray. </br> 
Right click on the SubutaiTray icon to open menu. Now You can open management dashbord with Launch->Launch to SS console menu.</br>


# Setup environment
You need the following tools to build the installer:
	<ul>
		<li> Visual Studio 2015 </li>
		<li> Visual Studio 2015 Installation Project extention</li>
		<li> 7-zip sowtware</li>
	</ul>


# Build the installer
Build Visual Studio project placed in vs_install folder and copy Deployment.exe to installation_files\bin folder.</br>
Build Visual Studio project placed in vs_uninstall\uninstall_clean folder and copy uninstall-clean.exe (it will run on uninstall) to installation_files\bin folder.</br>
Open Installation project in vs_setup folder. Right click on solution ->View->Custom Actions. Click on Deployment.exe, in Properties->Arguments type 3 arguments:  desired installation type ("prod/dev/master"), name of repo-descriptor file (repomd5) and "Install". Build Project.</br>
Two files will be created in bin/Release folder: Subutai.msi ans setup.exe. Copy both files into SubutaiInstaller\<InstallationType> folders. Create 7-zip archive inside folder and copy it into SubutaiInstaller folder (..).</br>
Copy file 7zS.sfx from 7-zip\bin folder to SubutaiInstaller folder. We need to create self-extracting archive and run setup.exe after uncompressing.</br>
From command-line interface execute command:</br>
`copy /b 7zS.sfx + config.txt + <archive_name>.7z subutai-network-installer<-installation type>.exe</br>`
Names for installers should be: subutai-network-installer.exe for production, subutai-network-installer-dev.exe for dev and subutai-network-installer-master.exe for master installations.</br>


# Overview
## Deployment Tool Parameters (case-sensitive):
<ul>
	<li>
		params - activities to perform during installation. Order of parameters does not matter. All parameters should be separated w/comma
		<ul>
			<li>deploy-redist - deploy redistributables (Google Chrome, Oracle VirtualBox, etc.)</li>
			<li>prepare-vbox - configure VirtualBox (create NAT network, import Snappy.ova)</li>
			<li>prepare-rh - configure resource host (install Subutai, import templates, etc.)</li>
			<li>deploy-p2p - deploy Subutai P2P service</li>
		</ul>
	<li>network-installation - can be true or false</li>
	<li>kurjunUrl - Kurjun CDN network URL</li>
	<li>repo_descriptor - file-descriptor of Kurjun CDN repository for Windows installer</li>
	<li>appDir - Subutai installation directory</li>
	<li>peer - can be "trial" or "rh-only". Identifies type of RH installation.</li>
	</li>
</ul>

### Parameters examples:
<ul>
	<li> params=deploy-redist,prepare-vbox,prepare-rh,deploy-p2p </li>
	<li> network-installation=true </li>
	<li> kurjunUrl=https://cdn.subut.ai/ </li>
	<li> repo_descriptor=repomd5 </li>
	<li> appDir=C:\Subutai </li>
	<li> peer=trial </li>
</ul>

## Repository descriptor (repomd5)
	i.e. bin | tray.zip | 1 | 0 | 1 |1|1|1
	tray.zip - file to download from Kurjun
	bin - is target directory where the file will be saved. I.e. if our Subutai directory is C:\Subutai then full path will be C:\Subutai\bin\tray.zip

"| 1 | 0 | 1 |1|1|1" describes if this file will needed for given peer type and installation type, 1 means fille need to be installed, 0 - file not needed:
| Trial | RH | Tray |prod|-dev|-master
peer type can be:
	 Trial: RH + Management + SubutaiTray, recomended for start
	 RH: RH only - will be needed for multy-RH installations (1 MH and many RH), recommended for advanced users, Subutai Tray can be installed if needed
	SubutaiTray: SubutaiTray application only. Installed if You have MH or RH installed before, and if You plan to work with environments on remote hosts.

## Full content of repomd5 (all these files must persist on Kurjun):
	bin | tray.zip - tray application
	bin | p2p.exe - Subutai P2P service
	bin | ssh.zip - ssh shell 
	ova | snappy.ova - Ubuntu Snappy ViratualBox image
	redist | chrome.msi - Google Chrome browser (https://www.google.com/work/chrome/browser/)
	redist | tap-driver.exe - TAP driver (https://swupdate.openvpn.org/community/releases/tap-windows-9.21.1.exe)
	redist | vcredist64.exe - Visual C++ Redistributables (https://www.microsoft.com/en-us/download/details.aspx?id=48145)
	redist | virtualbox.exe - Oracle VirtualBox (http://download.virtualbox.org/virtualbox/5.0.16/VirtualBox-5.0.16-105871-Win.exe)
	redist/subutai | subutai_4.0.<VN>_amd64.snap - Subutai package for Ubuntu Snappy 	
redist/subutai | subutai_4.0.<VN + 1>_amd64-dev.snap - Subutai package for Ubuntu Snappy built from dev branch
	redist/subutai | subutai_4.0.<VN + 1>_amd64-master.snap - Subutai package for Ubuntu Snappy built from master branch

Installation Manual can be found here: https://github.com/subutai-io/installers/wiki/Windows-Installer:-Installation-Manual

NOTE
#What need to be done if version changed (before release)
In nothing changed except version number:

1. Change 3 last lines of repo descriptor file: subutai_4.0.<VN>_amd64.snap  - VN is Version Number like 4.0.5; subutai_4.0.<VN + 1>_amd64-dev.snap - 4.0.6 if VN = 4.0.5; subutai_4.0.<VN + 1>_amd64-master.snap - 4.0.6.
2. Open Visual Studio installation project, change version number for prod, master and dev, build project for each installation type and build installer for each installation type as described above )Build the installer)
