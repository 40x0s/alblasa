@echo off
chcp 65001 > nul
echo.
echo  ╔═══════════════════════════════════╗
echo  ║   ميزان Pro — Build v3.0.0       ║
echo  ╚═══════════════════════════════════╝
echo.
echo [1/3] جاري استعادة الحزم...
dotnet restore
echo.
echo [2/3] جاري البناء...
dotnet build -c Release --no-restore
echo.
echo [3/3] جاري النشر...
dotnet publish -c Release -r win-x86 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  --no-build -o ./dist
echo.
echo  ✓ مكتمل! الملف: .\dist\MizanPro.exe
echo.
pause
