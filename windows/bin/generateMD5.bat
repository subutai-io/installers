@echo off
set SUBUTAI=G:\
set PATH=%PATH%;%SystemDrive%\Program Files\Oracle\VirtualBox;%SUBUTAI$/bin
cd %SUBUTAI%/bin
powershell -ExecutionPolicy ByPass -File ./autodeploy.ps1 -params generate-md5 -tomd5 G:\repomd5\