<div align="center">

[简体中文](https://github.com/ylLty/SelectUnknown/blob/master/README.md)  | English 


![icon](https://raw.githubusercontent.com/ylLty/SelectUnknown/refs/heads/master/docs/load-window.webp)
# Select Unknown
A Windows “select to search” tool integrating [Visual Search](#visualSearch), [Text Selection Search](#textSearch), [OCR](#OCR), [Translation](#translate), [QR Code Scan](#qrcodeScan), [Screenshot](#screenshot), and [Color Picker](#takeColour)

![GitHub Repo stars](https://img.shields.io/github/stars/ylLty/SelectUnknown?style=flat-square&color=%23D8B024) [![GitHub Release](https://img.shields.io/github/v/release/ylLty/SelectUnknown?style=flat-square&color=%231a7f37)](https://github.com/ylLty/SelectUnknown/releases/latest) [![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/ylLty/SelectUnknown/total?style=flat-square)](https://github.com/ylLty/SelectUnknown/releases/latest) [![TGCN](https://img.shields.io/badge/中文交流群-Telegram-blue.svg?style=flat-square&logo=telegram)](https://t.me/SelectUnknownCN) [![Static Badge](https://img.shields.io/badge/介绍视频-bilibili-pink?style=flat-square&logo=bilibili&logoColor=pink&labelColor=gray&color=pink)](https://www.bilibili.com/video/BV1csfBBoEEG/)

</div>

<div align="center">

# Table of Contents
</div>

- [Features Overview](#Features Overview)
- [Screenshots](#Screenshots)
- [Quick Start](#Quick Start)
- [Build Guide](#Build Guide)
- [Additional Notes](#Additional Notes)
- [Sponsor Development](#Sponsor Development)

<div align="center">

# Features Overview
|           Visual Search <a id="visualSearch"></a>            |        Text Selection Search <a id="textSearch"></a>         |                     OCR <a id="OCR"></a>                     |
| :----------------------------------------------------------: | :----------------------------------------------------------: | :----------------------------------------------------------: |
| ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/image-search-custom.png?raw=true) | ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/text-search-variant-custom.png?raw=true) | ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/alpha-t-box-outline-custom.png?raw=true) |
| Press the configured “Select Unknown” hotkey to bring up the interface, then drag to select an image area to perform reverse image search. Supports Google Lens and Yandex. Partial support for Baidu. | After selecting text in external applications (browsers, chats, etc.), press the configured “Select Unknown” hotkey to search directly within the Select Unknown window. Most major search engines are supported. If the selected text is a link, it will be opened directly in the default browser. | Press the configured “Select Unknown” hotkey to bring up the interface, then select text on the screen to automatically recognize it, fill it into the text processing box, and search via the search engine. The recognized text can also be copied after searching. |

|              Translation <a id="translate"></a>              |             QR Code Scan <a id="qrcodeScan"></a>             |              Screenshot <a id="screenshot"></a>              |
| :----------------------------------------------------------: | :----------------------------------------------------------: | :----------------------------------------------------------: |
| ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/translate-custom.png?raw=true) | ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/qrcode-scan-custom.png?raw=true) | ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/content-cut-custom.png?raw=true) |
| After opening the “Select Unknown” interface and choosing translation mode, select text to recognize and translate it. You can also directly translate text from the text processing box. | After opening the “Select Unknown” interface, QR codes on the screen will be automatically detected and displayed as cards. Left-click a card to open it, right-click to parse and view the raw QR code content. | Press the configured screenshot hotkey to quickly capture the screen. You can also locally save selected images within the Select Unknown window. |

|             Color Picker <a id="takeColour"></a>             |
| :----------------------------------------------------------: |
| ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/eyedropper-custom.png?raw=true) |
| After opening the “Select Unknown” interface and selecting the color picker tool, click any pixel on the screen to copy its color code to the clipboard. |

</div>

<div align="center">

# Screenshots
<img src="https://github.com/ylLty/SelectUnknown/blob/master/docs/screenshot.webp?raw=true" width="1000" />

</div>

<div align="center">

# Quick Start
</div>

### Download the Software

1. [Click here to download the latest release installer](https://github.com/ylLty/SelectUnknown/releases/latest)

2. Install according to the release notes

### (Optional) Download PaddleOCR-json (Better OCR Engine)

Installation Guide:

1. Download the PaddleOCR-json engine

Via [GitHub](https://github.com/aki-0929/PaddleOCRJson.NET/releases/download/default_runtime/default_runtime.zip) (limited access in mainland China) or [Quark Cloud Drive](https://pan.quark.cn/s/6470fd185c55)

2. Open the `PaddleOCR-json` folder in the software root directory

3. Extract all files from `default_runtime.zip` into the `PaddleOCR-json` folder

4. Re-select the engine

Then the software is ready to run.

<div align="center">

# Build Guide
</div>

This project needs to be built using Visual Studio 2022.

Simply clone this repository and build the project.

If you want to distribute an `.msi` installer, please refer to this document:
[Visual Studio Installer Projects and .NET | Microsoft Learn](https://learn.microsoft.com/en-us/visualstudio/deployment/installer-projects-net-core?view=visualstudio)

<div align="center">

# Additional Notes
</div>

>[!WARNING]
>**Do NOT use the “select to search” feature when sensitive, private, or confidential information is displayed on the screen**

- This project is licensed under the [GPL-3.0 Open Source License](https://github.com/ylLty/SelectUnknown/blob/master/LICENSE.txt). [Click here to view open source licenses](https://github.com/ylLty/SelectUnknown/blob/master/docs/open-source-license.txt).
- Currently only Windows 10/11 are supported. Linux is not supported at this time. The project may be rewritten using Electron in the future ~~(just a plan for now)~~.

<div align="center">

# Sponsor Development
</div>

If you find my software useful, consider buying me a coffee ☕

You can sponsor via the following two methods:

### 1. Sponsor via WeChat Pay or Alipay

<img src="https://github.com/ylLty/SelectUnknown/blob/master/docs/donate-by-alipay.webp?raw=true" width="300" /> <img src="https://github.com/ylLty/SelectUnknown/blob/master/docs/donate-by-wechat.webp?raw=true" width="330" />

Please add the note: `Sponsor Select Unknown - [your GitHub ID]`.  
All sponsors will be listed on [this page](https://github.com/ylLty/SelectUnknown/blob/master/docs/list-of-Sponsors.md)!  
(If you don’t have a GitHub account, you may use a nickname.)

If you wish to sponsor anonymously, please use the note `Sponsor Select Unknown` **without adding personal information**.  
**Otherwise, I will be unable to determine the intent of the donation, and it will NOT be included in the sponsor list!**

**Refunds are not supported at this time.**

### 2. Sponsor the developer via Afdian

[Click here](https://afdian.com/a/yl_lty) to open the Afdian sponsorship page.

All sponsors will appear on [this list](https://github.com/ylLty/SelectUnknown/blob/master/docs/list-of-Sponsors.md) (anonymous sponsorship supported).  
Thank you for your support ❤️