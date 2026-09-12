@echo off
chcp 65001 > nul
echo.
echo  ╔═══════════════════════════════════╗
echo  ║   ميزان Pro — Workshop Pack        ║
echo  ╚═══════════════════════════════════╝
echo.

rem ═══ التحقق من وجود الملف التنفيذي ═══
if not exist "dist\MizanPro.exe" (
    echo  ✗ لم يتم العثور على dist\MizanPro.exe
    echo    الرجاء تشغيل build.bat أولاً لإنتاج النسخة المنشورة.
    echo.
    pause
    exit /b 1
)

rem ═══ إعادة إنشاء مجلد التسليم ═══
if exist "Workshop_Delivery" rmdir /s /q "Workshop_Delivery"
mkdir "Workshop_Delivery" || goto :error

echo  [1/4] نسخ البرنامج...
copy /y "dist\MizanPro.exe" "Workshop_Delivery\MizanPro.exe" > nul || goto :error

echo  [2/4] نسخ دليل الورشة...
copy /y "README_Workshop.md" "Workshop_Delivery\README_Workshop.md" > nul || goto :error

echo  [3/4] نسخ روابط الأدوات...
copy /y "Tools_Links.txt" "Workshop_Delivery\Tools_Links.txt" > nul || goto :error

echo  [4/4] نسخ ورقة التجارب...
copy /y "LabSheet.md" "Workshop_Delivery\LabSheet.md" > nul || goto :error

echo.
echo  ───────────────────────────────────
echo   محتويات حزمة التسليم:
echo  ───────────────────────────────────
dir /b "Workshop_Delivery"
echo  ───────────────────────────────────
echo.
echo  ✓ اكتمل! الحزمة جاهزة في: .\Workshop_Delivery\
echo.
pause
exit /b 0

:error
echo.
echo  ✗ حدث خطأ أثناء تجميع الحزمة — تأكد من وجود جميع الملفات.
echo.
pause
exit /b 1
