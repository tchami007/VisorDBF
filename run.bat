@echo off
cd /d "%~dp0"
dotnet run --project src\VisorDBF.UI\VisorDBF.UI.csproj -c Debug
pause
