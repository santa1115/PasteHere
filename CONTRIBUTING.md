# Contributing to PasteHere

Thank you for your interest in contributing to PasteHere! We welcome contributions from everyone.

## 🧠 Core Philosophy
PasteHere is defined by its **minimalism**.
*   **Size Matters**: The executable strives to stay under **20KB**.
*   **No Dependencies**: We do NOT use external NuGet packages or DLLs unless absolutely necessary. We rely on the native .NET Framework built into Windows.
*   **Zero Footprint**: The app must not run in the background. It runs once when clicked, and exits immediately.

## 🛠️ How to Build
You don't need Visual Studio installed. You just need a Windows PC.
1.  Open the project folder.
2.  Double-click `build.bat` (for English version) or `build_cn.bat` (for Chinese version).
3.  The C# Compiler (`csc.exe`) will automatically build the `.exe`.

## 🐛 Reporting Bugs
Please use the **Issue Tracker** on GitHub.
*   Describe the OS version (Win 10/11).
*   Describe what you copied (Image? Text? URL?).
*   Provide screenshots if possible.

## 🧩 Pull Requests
1.  Fork the repo.
2.  Create a new branch (`git checkout -b feature/AmazingFeature`).
3.  Commit your changes.
4.  **Test locally**: Ensure `build.bat` still passes and the file size hasn't exploded.
5.  Open a Pull Request.

Thank you for helping make PasteHere better!
