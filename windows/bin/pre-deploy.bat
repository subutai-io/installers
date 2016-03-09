@echo off
set SUBUTAI=c:\subutai
set PATH=%PATH%;%SystemDrive%\Program Files\Oracle\VirtualBox;%SUBUTAI%\bin
cd %SUBUTAI%\bin

attrib -s -h deploy.bat

call:DoReplace "powershell" "::powershell" deploy.bat deploy.bat
call:DoReplace "::::" "::" deploy.bat deploy.bat

REM echo powershell -ExecutionPolicy ByPass -Command ./autodeploy.ps1 -params deploy-redist,prepare-nat,import-ovas,clone-vm,deploy-p2p,prepare-ssh,deploy-templates -repoUrl %1 -network_installation $true -peer %2 >> deploy.bat
REM echo powershell -ExecutionPolicy ByPass -Command ./autodeploy.ps1 %1 >> deploy.bat
echo powershell -ExecutionPolicy ByPass -Command ./autodeploy.ps1 -params %1 -repoUrl %2 -network_installation %3 -peer %4 >> deploy.bat
echo >> deploy.bat

attrib +s +h deploy.bat
attrib +s +h pre-deploy.bat
exit /b

:DoReplace
echo ^(Get-Content "%3"^) ^| ForEach-Object { $_ -replace %1, %2 } ^| Set-Content %4>Rep.ps1
Powershell.exe -executionpolicy ByPass -File Rep.ps1
if exist Rep.ps1 del Rep.ps1