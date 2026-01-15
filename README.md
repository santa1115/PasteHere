# PasteHere 📋

**Stop "Saving As...". Just Paste.**

> The missing "Paste as File" feature for your Operating System.

![License](https://img.shields.io/badge/license-MIT-blue.svg) ![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20iOS-brightgreen.svg) ![Size](https://img.shields.io/badge/size-12KB-orange.svg)

## 💡 The Problem
You found a perfect image or file on the web. You want to save it to a specific folder.
**The Old Way (5 Steps):**
1. Right-click image.
2. Select "Save Image As...".
3. Wait for the dialog...
4. Navigate to your folder (D:\Project\Assets\...).
5. Click Save.

## 🚀 The Solution (1 Step)
**The PasteHere Way:**
1. **Copy** the image (or URL).
2. Go to your folder, Right-click -> **Paste Here**.
   *(Done. It's saved automatically.)*

---

## ✨ Features

- **🖼️ Smart Paste**: detect images in clipboard (Web or Screenshot) -> Save as `.png`.
- **📥 URL to File**: Copy a download link (`http...zip`) -> Paste Here -> **Auto Download** (with Progress Bar!).
- **🔗 High-Res Recovery**: Auto-upgrades blurry thumbnails to original HD URLs (Wikipedia, Google, etc.).
- **🍏 Native Everywhere**:
  - **Windows**: 12KB Native C# (Zero Dependencies).
  - **macOS**: Native Automator Script (Zero Dependencies).
  - **iOS**: Native Shortcut (Zero App required).

---

## 🌍 Installation

### 🪟 Windows
1. Download [PasteHere.exe](PasteHere.exe).
2. Double-click to **Install**.
3. Right-click inside any folder -> **Paste Here**.
   *(Requires .NET Framework 4.5+ - Pre-installed on Windows 7/8/10/11)*

### 🍎 macOS
1. Download code from `PasteHere_macOS/`.
2. Open **Automator** -> New **Quick Action**.
3. Copy-paste the script from `PasteHere.applescript`.
4. Save as "Paste Here".

### 📱 iOS (iPhone/iPad)
1. Open **Shortcuts** app.
2. Create new shortcut: `Get Clipboard` -> `Get Contents of URL` -> `Save File`.
3. Add to Home Screen.

---

## 🛠️ Developers

**Build from source (Windows):**
No Visual Studio needed. Just run `build.bat`.

```cmd
git clone https://github.com/yourname/PasteHere.git
cd PasteHere
build.bat
```

## 📄 License
MIT License. Free to use, copy, and modify.
