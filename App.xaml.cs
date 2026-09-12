using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using MizanPro.Core;
using MizanPro.Core.Engine;
using MizanPro.Core.Services;
using MizanPro.Data;
using MizanPro.Windows;

namespace MizanPro
{
    /// <summary>
    /// نقطة الدخول الرئيسية للتطبيق.
    /// تسلسل التشغيل:
    ///   1) إنشاء قاعدة البيانات وتطبيق الـ Migrations (+ تعبئة البيانات عند أول تشغيل)
    ///   2) شاشة البداية لمدة 3 ثوانٍ
    ///   3) فحص حالة المنتج عبر ProductStateEngine.EvaluateCurrentState()
    ///   4) تسجيل الدخول ثم فتح النافذة الرئيسية
    /// </summary>
    public partial class App : Application
    {
        /// <summary>مدة عرض شاشة البداية بالمللي ثانية (3 ثوانٍ).</summary>
        private const int SplashDurationMs = 3000;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // التقاط أي استثناء غير معالج في واجهة المستخدم وعرضه بدل انهيار التطبيق
            DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show(
                    "حدث خطأ غير متوقع:\n" + args.Exception.Message,
                    "ميزان برو",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                args.Handled = true;
            };

            try
            {
                // ══════════ 1) قاعدة البيانات ══════════
                MizanPaths.EnsureAppDataFolder();

                var splash = new SplashWindow();
                splash.Show();

                var stopwatch = Stopwatch.StartNew();

                using (var db = new MizanDbContext())
                {
                    // إنشاء قاعدة البيانات (إن لم تكن موجودة) وتطبيق الـ Migrations
                    await db.Database.MigrateAsync();

                    // تعبئة البيانات التجريبية عند أول تشغيل فقط
                    if (!await db.Users.AnyAsync())
                        await DataSeeder.SeedAsync(db);
                }

                // ══════════ 2) فحص حالة المنتج (تجربة / تفعيل) ══════════
                // true  = نسخة PRO أو مسار احتياطي صالح أو تجربة متبقية
                // false = انتهت الفترة التجريبية (7 أيام) بدون تفعيل صالح
                bool canRun = ProductStateEngine.Instance.EvaluateCurrentState();

                // إبقاء شاشة البداية ظاهرة 3 ثوانٍ على الأقل منذ لحظة ظهورها
                var remainingMs = SplashDurationMs - (int)stopwatch.ElapsedMilliseconds;
                if (remainingMs > 0)
                    await Task.Delay(remainingMs);

                // انتهت الفترة التجريبية ولم يتم التفعيل → إغلاق التطبيق
                if (!canRun)
                {
                    splash.Close();

                    MessageBox.Show(
                        "انتهت الفترة التجريبية المجانية (7 أيام).\n" +
                        "الرجاء التواصل مع المورّد للحصول على مفتاح تفعيل صالح.",
                        "ميزان برو",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.OK,
                        MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                    Shutdown();
                    return;
                }

                // ══════════ 3) تسجيل الدخول ══════════
                // canRun == true: النسخة مفعّلة (PRO) أو داخل الفترة التجريبية.
                // لوحة التفعيل متاحة دائماً داخل نافذة الدخول عبر زر
                // "لديّ مفتاح تفعيل" — لذا نمرر needsActivation: false هنا.
                var auth = new AuthWindow(needsActivation: false);
                auth.Show();
                splash.Close();

                // ══════════ 4) القرار بعد إغلاق نافذة الدخول ══════════
                auth.Closed += (sender, args) =>
                {
                    if (SessionManager.Instance.IsAuthenticated)
                    {
                        // نجح تسجيل الدخول → فتح النافذة الرئيسية
                        var main = new MainWindow();
                        main.Show();
                    }
                    else
                    {
                        // أُغلقت نافذة الدخول بدون تسجيل → إيقاف التطبيق
                        Shutdown();
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "تعذّر تشغيل البرنامج:\n" + ex.Message,
                    "ميزان برو",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

                Shutdown(-1);
            }
        }
    }
}
