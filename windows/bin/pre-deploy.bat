@echo off
set SUBUTAI=%6
set PATH=%PATH%;%SystemDrive%\Program Files\Oracle\VirtualBox;%SUBUTAI%\bin
cd %SUBUTAI%\bin

attrib -s -h deploy.bat

call:DoReplace "powershell" "::powershell" deploy.bat deploy.bat
call:DoReplace "::::" "::" deploy.bat deploy.bat

echo @echo off > deploy.bat
echo set SUBUTAI=%6 >> deploy.bat
echo cd %SUBUTAI%\bin >> deploy.bat
::echo set PATH=%PATH%;%SystemDrive%\Program Files\Oracle\VirtualBox;%SUBUTAI%\bin >> deploy.bat
echo powershell -ExecutionPolicy ByPass -Command ./autodeploy.ps1 -params %1 -repoUrl %2 -network_installation %3 -peer %4 -services %5 -appdir %6 >> deploy.bat
::echo ./autodeploy.exe -params %1 -repoUrl %2 -network_installation %3 -peer %4 -services %5 -appdir %6 >> deploy.bat

attrib +s +h deploy.bat
attrib +s +h pre-deploy.bat
exit /b

:DoReplace
echo ^(Get-Content "%3"^) ^| ForEach-Object { $_ -replace %1, %2 } ^| Set-Content %4>Rep.ps1
Powershell.exe -executionpolicy ByPass -File Rep.ps1
if exist Rep.ps1 del Rep.ps1