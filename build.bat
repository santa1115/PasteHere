@echo off
:: PasteHere Build Script
:: Uses the native .NET C# Compiler (csc.exe) to build a standalone executable.
:: No External Dependencies required.

setlocal

:: Find latest .NET Framework path
set "FrameworkDir=%SystemRoot%\Microsoft.NET\Framework64"
for /f "delims=" %%D in ('dir /b /ad /o-n "%FrameworkDir%\v4.0*"') do (
    set "DOTNET_PATH=%FrameworkDir%\%%D"
    goto :Found
)

:Found
echo Found .NET Compiler at: %DOTNET_PATH%

:: Compile
"%DOTNET_PATH%\csc.exe" /t:winexe /out:PasteHere.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Net.dll /r:System.Web.dll Program.cs

if %errorlevel% equ 0 (
    echo.
    echo [SUCCESS] PasteHere.exe created successfully!
) else (
    echo.
    echo [ERROR] Compilation failed.
)
