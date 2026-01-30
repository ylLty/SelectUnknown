@echo off
REM prebuild.bat - 简化写死版本
REM 假设本批处理和项目文件在同一目录

set "RES_SOURCE=%~dp0res"
set "RES_TARGET=%~dp0bin\Debug\net8.0-windows\res"

echo 源目录: %RES_SOURCE%
echo 目标目录: %RES_TARGET%

if not exist "%RES_SOURCE%\" (
    echo 错误: res 文件夹不存在
    pause
    exit /b 1
)

REM 创建目标目录
if not exist "%RES_TARGET%\" (
    mkdir "%RES_TARGET%"
)

REM 复制所有内容
xcopy "%RES_SOURCE%\*" "%RES_TARGET%\" /E /Y /I /Q

echo 复制完成