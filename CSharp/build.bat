@echo off
chcp 65001 >nul
echo [1/2] 正在检测 Windows 内置 C# 编译器...

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist %CSC% (
    set CSC="C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if not exist %CSC% (
    echo [错误] 未找到系统内置的 csc.exe 编译器！
    pause
    exit /b 1
)

echo [2/2] 正在编译 文本加密助手.exe ...
%CSC% /target:winexe /optimize+ /platform:anycpu /win32icon:"app_icon.ico" /out:"文本加密助手.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll /r:System.Core.dll "Program.cs"

if %errorlevel% equ 0 (
    echo.
    echo =======================================================
    echo  编译成功！生成文件：文本加密助手.exe
    echo  体积仅 ~145 KB，双击即可极速秒开运行。
    echo =======================================================
) else (
    echo.
    echo [错误] 编译失败，请检查报错信息。
)
pause
