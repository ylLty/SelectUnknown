<p align="center"><img src="https://raw.githubusercontent.com/ylLty/SelectUnknown/refs/heads/master/docs/load-window.webp" alt="ShareX Banner" width="800"/></p>
<div align="center">

简体中文 | [English](https://github.com/ylLty/SelectUnknown/blob/master/docs/README_en.md)
# Select Unknown - 框索未知
一个集成了[视觉搜索](#visualSearch)、[划词搜索](#textSearch)、[文字识别](#OCR)、[翻译](#translate)、[扫码](#qrcodeScan)、[截图](#screenshot)、[取色](#takeColour)的 Windows 版 “圈定即搜”

![GitHub Repo stars](https://img.shields.io/github/stars/ylLty/SelectUnknown?style=flat-square&color=%23D8B024) [![GitHub Release](https://img.shields.io/github/v/release/ylLty/SelectUnknown?style=flat-square&color=%231a7f37)](https://github.com/ylLty/SelectUnknown/releases/latest) [![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/ylLty/SelectUnknown/total?style=flat-square)](https://github.com/ylLty/SelectUnknown/releases/latest) [![TGCN](https://img.shields.io/badge/交流群-Telegram-blue.svg?style=flat-square&logo=telegram)](https://t.me/SelectUnknownCN) [![Static Badge](https://img.shields.io/badge/%E4%BB%8B%E7%BB%8D%E8%A7%86%E9%A2%91-bilibili-pink?style=flat-square&logo=bilibili&logoColor=pink&labelColor=gray&color=pink)](https://www.bilibili.com/video/BV1csfBBoEEG/)



</div>

<div align="center">

# 目录
</div>

- [特性一览](#特性一览)
- [软件截图](#软件截图)
- [快速开始](#快速开始)
- [生成指南](#生成指南)
- [其他说明](#其他说明)
- [赞助开发](#赞助开发)

<div align="center">

# 特性一览
| 视觉搜索 <a id="visualSearch"></a> | 划词搜索 <a id="textSearch"></a> | 文字识别 <a id="OCR"></a> |
| :---: | :---: | :---: |
| ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/image-search-custom.png?raw=true) | ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/text-search-variant-custom.png?raw=true) | ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/alpha-t-box-outline-custom.png?raw=true) |
| 按下设定的 “框索未知” 快捷键，呼出界面后，使用鼠标框中要搜索的图片即可调用以图搜图引擎搜索。支持 Google Lens, Yandex. 半支持百度。 | 在外部（浏览器、聊天等）选中文字后，按下设定的 “框索未知” 快捷键，会直接在框索未知窗口中搜索。支持大多主流搜索引擎。若选中文字为链接则会直接用默认浏览器打开链接。 | 按下设定的 “框索未知” 快捷键，呼出界面后，框选屏幕中的文字即可自动识别填入文字处理框并调用搜索引擎搜索文字。搜索完成也可以复制识别的文字。 |

| 翻译 <a id="translate"></a> | 扫码 <a id="qrcodeScan"></a> | 截图 <a id="screenshot"></a> |
| :---: | :---: | :---: |
| ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/translate-custom.png?raw=true) | ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/qrcode-scan-custom.png?raw=true) | ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/content-cut-custom.png?raw=true) |
| 呼出 “框索未知” 界面，选中翻译模式后，框选文本即可识别并调用翻译。同时也可以直接翻译文字处理框中的文字。 | 呼出 “框索未知” 界面后，会自动识别屏幕中的二维码并弹出卡片，左键点击卡片即可进入，右键卡片解析二维码源内容。 | 按下设定的截图快捷键即可快速截取屏幕。在框索未知窗口中也可以局部保存框中图片 |

| 取色 <a id="takeColour"></a> |
|:---:|
| ![](https://github.com/ylLty/SelectUnknown/blob/master/docs/icons-for-doc/eyedropper-custom.png?raw=true) |
| 呼出 “框索未知” 界面，选中取色工具后，点击屏幕中的像素点即可复制色号到剪切板。 |

</div>

<div align="center">

# 软件截图
<img src="https://github.com/ylLty/SelectUnknown/blob/master/docs/screenshot.webp?raw=true" width="1000" />

</div>

<div align="center">

# 快速开始
</div>

### 下载软件

1、[点此下载最新版安装包](https://github.com/ylLty/SelectUnknown/releases/latest)

2、根据发行说明安装

### （额外）下载 PaddleOCR-json （更优秀的 OCR 引擎）

安装指导：

1、下载 PaddleOCR-json 引擎

通过 [GitHub](https://github.com/aki-0929/PaddleOCRJson.NET/releases/download/default_runtime/default_runtime.zip) （国内访问受限） 或 [夸克网盘](https://pan.quark.cn/s/6470fd185c55) 

2、打开软件根目录中 PaddleOCR-json 文件夹

3、将 default_runtime.zip 中的所有文件解压于 PaddleOCR-json 文件夹中

4、重新选择引擎"

 

然后就可以运行了


<div align="center">

# 生成指南
</div>

本项目需要使用 Visual Studio 2022 生成

克隆本仓库后生成即可

若要发行 .msi 的安装程序，请参见此文档：[Visual Studio 安装程序项目和 .NET | Microsoft Learn](https://learn.microsoft.com/zh-cn/visualstudio/deployment/installer-projects-net-core?view=visualstudio)

<div align="center">

# 其他说明
</div>

>[!WARNING]
>**若屏幕正在显示隐私、敏感或机密等信息时，请勿使用框定即搜功能**

- 本项目使用 [GPL-3.0 开源许可证](https://github.com/ylLty/SelectUnknown/blob/master/LICENSE.txt)。[点此查看开放源代码许可](https://github.com/ylLty/SelectUnknown/blob/master/docs/open-source-license.txt)。
- 目前仅支持 Windows 10/11 系统使用，Linux 系统暂不支持。未来可能会用 Electron 重写此项目 ~~（画饼中）~~

<div align="center">

# 赞助开发
</div>

如果你觉得我的软件好用，请给我一杯咖啡

可通过下列两种方式赞助

### 1、通过微信支付或支付宝赞助

<img src="https://github.com/ylLty/SelectUnknown/blob/master/docs/donate-by-alipay.webp?raw=true" width="300" /> <img src="https://github.com/ylLty/SelectUnknown/blob/master/docs/donate-by-wechat.webp?raw=true" width="330" />

请备注`赞助 Select Unknown - [你的github ID]`。所有赞助者都将公布于[此名单](https://github.com/ylLty/SelectUnknown/blob/master/docs/list-of-Sponsors.md)！(没有Github账号可以填写昵称)

若你想匿名赞助，请填写备注`赞助 Select Unknown`但不备注你的信息，**否则我无法判断收款码收款意图！将不会纳入赞助名单！**

**暂不支持退款**。

### 2、通过爱发电赞助开发者

[点击这里](https://afdian.com/a/yl_lty)打开爱发电赞助页面

所有赞助者将出现在[此名单](https://github.com/ylLty/SelectUnknown/blob/master/docs/list-of-Sponsors.md)中（可备注匿名），感谢你们的支持。
