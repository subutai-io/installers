@echo off
::set SUBUTAI=C:\Subutai
::set PATH=%PATH%;%SystemDrive%\Program Files\Oracle\VirtualBox;%SUBUTAI%\bin
::cd %SUBUTAI%\bin
::powershell -ExecutionPolicy ByPass -Command ./autodeploy.ps1 -params deploy-redist,prepare-nat,import-ovas,clone-vm,deploy-p2p,prepare-ssh,deploy-templates -repoUrl %1 -network_installation $true -peer %2
