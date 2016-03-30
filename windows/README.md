# Subutai Windows Installer

# How to install
Run installer w/Administrative privileges on fresh Windows 7/8/10 x64 machine </br>
Wait until installation process and shell scripts finish installation </br>
Now you can go to Chrome browser and open management dashbord https://localhost:9999</br>


# Setup environment
You need the following tools to build the installer:
	<ul>
	<li> Advanced Installer 12 </li>
	<li> Visual Studio 2008 / 2013 / 2015 </li>
	<li> DevExpress 2016 </li>
	</ul>

# Build the installer
	Build Visual Studio project and copy Deployment.exe to \bin folder of Advanced Installer project
	Run the Build process from Advanced installer (you can use CLI http://www.advancedinstaller.com/user-guide/command-line.html)

# Test the installer
	You can Build and Run the installer inside VM right from Advanced installer

# Overview
	We use Advanced Installer as wrapper - it packs all required files (scripts, utilities) into single installer which is delivering to an end-user.
	Features:
	<ul>
		<li> Check environment compatibility </li>
		<li> Deploy required system components if required </li>
		<li> Copy installation files i.e. scripts, etc. </li>
		<li> Initialization of autorun </li>
	</ul>

	And we use Deployment.exe tool developed under Visual Studio to handle the second part of installation.
	## Features:
	<ul>
		<li> Download prerequisites from Kurjun </li>
		<li> Verify MD5 checksums </li>
		<li> Configuring resourse host </li>
		<li> Setting up P2P service </li>
	</ul>

	## Flags (case-sensitive):
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

	## Flags examples:
	<ul>
		<li> params=deploy-redist,prepare-vbox,prepare-rh,deploy-p2p </li>
		<li> network-installation=true </li>
		<li> kurjunUrl=https://kurjun.cdn.subutai.io:8338/ </li>
		<li> repo_descriptor=repomd5 </li>
		<li> appDir=C:\Subutai </li>
		<li> peer=trial </li>
	</ul>

	## Repository descriptor (repomd5)
	i.e. bin | tray.zip
	tray.zip - file to download from Kurjun
	bin - is target directory where the file will be saved. I.e. if our Subutai directory is C:\Subutai then full path will be C:\Subutai\bin\tray.zip