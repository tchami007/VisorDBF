@echo off
cd /d "%~dp0"

REM === Development ===
REM   run
REM
REM === Build distribution (self-contained win-x64) ===
REM   dotnet publish src\VisorDBF.UI -c Release -r win-x64 --self-contained true ^
REM     -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
REM     -p:DebugType=none -o publish
REM
REM   Then run: publish\VisorDBF.exe

dotnet run --project src\VisorDBF.UI\VisorDBF.UI.csproj -c Debug
pause
