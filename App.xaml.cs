using System.Windows;
using MizanPro.Core;
using MizanPro.Core.Services;
using MizanPro.Windows;

namespace MizanPro
{
    /// <summary>
    /// نقطة الدخول الرئيسية للتطبيق.
    /// تسلسل التشغيل:
    ///   1) شاشة البداية تنفّذ: قاعدة البيانات + Migrations + البيانات التجريبية + فحص الترخيص
    ///   2) فشل التهيئة → رسالة وإغلاق | انتهت التجربة → رسالة وإغلاق
    ///   3) نافذة تسجيل الدخول (وتتضمن وضع التفعيل)
    ///   4) بعد تسجيل الدخول → النافذة الرئيسية
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
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
                MizanPaths.EnsureAppDataFolder();

                var splash = new SplashWindow();

                splash.WorkCompleted += (sender, result) =>
                {
                    splash.BeginFadeOut(() =>
                    {
                        // خطأ فادح أثناء التهيئة
                        if (result.Error is not null)
                        {
                            ShowFatal(result.Error.Message);
                            return;
                        }

                        // انتهت الفترة التجريبية ولم يتم التفعيل → إغلاق التطبيق
                        if (!result.CanRun)
                        {
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

                        // فتح نافذة الدخول (تحتوي أيضاً على وضع تفعيل المنتج)
                        var auth = new AuthWindow();
                        auth.Show();

                        // القرار بعد إغلاق نافذة الدخول
                        auth.Closed += (_, _) =>
                        {
                            if (SessionManager.Instance.IsAuthenticated)
                                new MainWindow().Show();
                            else
                                Shutdown();
                        };
                    });
                };

                splash.Show();
            }
            catch (Exception ex)
            {
                ShowFatal(ex.Message);
            }
        }

        private void ShowFatal(string message)
        {
            MessageBox.Show(
                "تعذّر تشغيل البرنامج:\n" + message,
                "ميزان برو",
                MessageBoxButton.OK,
                MessageBoxImage.Error,
                MessageBoxResult.OK,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

            Shutdown(-1);
        }
    }
}
