@echo off
for /f "tokens=3" %%i in ('reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" ^| findstr "Install.*0x"') do (set INST=%%i)
echo INST=%INST%
if "%INST%" == "0x1" (
 echo .Net Framework 4.5 already installed
) else (
 echo installing .Net Framework 4.5
 pause
 %1\Framework\dotNetFx45_Full_setup.exe /norestart /passive /showrmu
)
