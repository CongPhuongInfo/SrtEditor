@echo off
setlocal

REM =========================================================
REM  Build script cho SrtSsmlEditor (VB.NET, .NET Framework 4.x)
REM  Bien dich bang vbc.exe co san trong Windows, khong can
REM  cai Visual Studio.
REM =========================================================

set "VBC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\vbc.exe"
if not exist "%VBC%" set "VBC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\vbc.exe"

if not exist "%VBC%" (
    echo [LOI] Khong tim thay vbc.exe.
    echo Hay cai .NET Framework 4.x, hoac sua duong dan VBC trong file build.bat nay.
    pause
    exit /b 1
)

echo Dang dung: %VBC%
echo Dang bien dich SrtSsmlEditor.exe ...
echo.

"%VBC%" /nologo /target:winexe /out:SrtSsmlEditor.exe /optimize+ /rootnamespace:SrtSsmlEditor ^
    /reference:System.dll,System.Core.dll,System.Windows.Forms.dll,System.Drawing.dll,System.Xml.dll,System.Data.dll ^
    SrtEntry.vb MainForm.vb Program.vb

if errorlevel 1 (
    echo.
    echo [LOI] Bien dich that bai. Xem thong bao loi phia tren.
    pause
    exit /b 1
)

echo.
echo ============================================
echo  Bien dich thanh cong: SrtSsmlEditor.exe
echo ============================================
pause
