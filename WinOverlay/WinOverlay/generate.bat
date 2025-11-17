@echo off
setlocal enabledelayedexpansion

echo Syncing ShareClass.proto to ShareClass_Unity.proto...

REM Read ShareClass.proto and replace namespace
(
  for /f "usebackq delims=" %%a in (".\Proto\ShareClass.proto") do (
    set "line=%%a"
    echo !line! | findstr /C:"option csharp_namespace" >nul
    if !errorlevel! equ 0 (
      echo option csharp_namespace = "Share";
    ) else (
      echo !line!
    )
  )
) > ".\Proto\ShareClass_Unity.proto"

echo Sync complete!
echo.
echo Generating protobuf files...
echo.

REM Generate for WinOverlay project with WinOverlay namespace
echo [1/2] Generating for WinOverlay (namespace: WinOverlay)...
protoc --csharp_out=./Generated ./Proto/ShareClass.proto
if %errorlevel% neq 0 (
    echo Error generating WinOverlay protobuf files!
    exit /b %errorlevel%
)

REM Generate for Unity project with Share namespace
echo [2/2] Generating for Unity (namespace: Share)...
protoc --csharp_out=../../Assets/Scripts/Generated ./Proto/ShareClass_Unity.proto
if %errorlevel% neq 0 (
    echo Error generating Unity protobuf files!
    exit /b %errorlevel%
)

echo.
echo All protobuf files generated successfully!
echo - WinOverlay: ./Generated (namespace: WinOverlay)
echo - Unity: ../../Assets/Scripts/Generated (namespace: Share)