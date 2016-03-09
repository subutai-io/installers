@echo off
cd %SUBUTAI%/bin
powershell -ExecutionPolicy ByPass -File ./autodeploy.ps1 -params uninstall