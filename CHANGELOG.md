# Changelog

All notable changes to the **PasteHere** project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [V3.3.1] - 2026-01-16
### Changed
- **Messaging**: Renamed Chinese context menu item from "在此处粘贴图片" to "粘贴文件" (Paste File) for accuracy.
- **Credit**: Added "Author: Hua" signature to success messages and website footer.
- **Website**: Added "How to Use" guide and "Star Us" call-to-action to the landing page.

## [V3.3.0] - 2026-01-15
### Added
- **Smart Downloads**: Automatically detects file URLs in clipboard and downloads them with a progress bar.
- **TLS 1.2 Support**: Forced `SecurityProtocol = 3072` to fix downloads from modern sites (GitHub, Wikipedia).
- **Multi-Platform Assets**: Added scripts for macOS (`PasteHere.applescript`) and iOS Shortcuts guide.

## [V3.0.0] - 2026-01-14
### Changed
- **Major Rewrite**: Complete migration from Python to Native C# (.NET Framework 4.5).
- **Performance**: Executable size reduced from ~10MB to **13KB**. Startup is now instant.
- **Structure**: Integrated installer and paster into a single executable.

## [V2.0.0] - 2026-01-12
### Added
- **Smart Images**: Added logic to handle Wikipedia thumbnails (getting high-res originals).
- **Text Support**: Paste text directly as `.txt` files.

## [V1.0.0] - 2026-01-10
### Initial Release
- Python MVP using `Pillow` and `ctypes`.
- Basic functionality: Save clipboard image to file.
