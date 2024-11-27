@echo off

REM Add autorun.reg to the registry
regedit /s autorun.reg

REM Start screentime.exe from the current folder
start "" "%~dp0screentime.exe"