@echo off
cd %SUBUTAI%

powershell -ExecutionPolicy ByPass -Command ./bin/autodeploy.ps1 -params clean-after-uninstall
net stop "Subutai Social P2P"
nssm remove "Subutai Social P2P" confirm
FOR /D %%p IN ("%SUBUTAI%\*.*") DO rmdir "%%p" /s /q > nul 2>&1
exit 0