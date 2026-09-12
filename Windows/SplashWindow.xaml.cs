using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.EntityFrameworkCore;
using MizanPro.Core.Engine;
using MizanPro.Data;

namespace MizanPro.Windows
{
    /// <summary>نتيجة أعمال التهيئة التي تجري داخل شاشة البداية.</summary>
    public sealed class SplashResult
    {
        /// <summary>هل يمكن تشغيل البرنامج؟ (مفعّل أو داخل التجربة)</summary>
        public bool CanRun { get; set; }

        /// <summary>خطأ فادح أثناء التهيئة إن وُجد.</summary>
        public Exception? Error { get; set; }
    }

    /// <summary>
    /// شاشة البداية — تنفّذ أعمال التهيئة في خيط خلفي (BackgroundWorker)
    /// مع مراحل تقدم:
    ///   0→25%:  جاري التحقق من قاعدة البيانات (Migrations)
    ///   25→55%: تحميل بيانات الشركة (DataSeeder عند أول تشغيل)
    ///   55→80%: التحقق من حالة الترخيص (ProductStateEngine)
    ///   80→100%: مرحباً بك!
    /// ثم تتلاشى (Opacity 1→0 خلال 400ms) قبل الإغلاق.
    /// </summary>
    public partial class SplashWindow : Window
    {
        /// <summary>يُطلق على خيط الواجهة عند انتهاء أعمال التهيئة.</summary>
        public event EventHandler<SplashResult>? WorkCompleted;

        private readonly BackgroundWorker _worker = new();
        private readonly SplashResult _result = new();
        private bool _fadeStarted;

        public SplashWindow()
        {
            InitializeComponent();

            _worker.WorkerReportsProgress = true;
            _worker.DoWork += Worker_DoWork;
            _worker.ProgressChanged += Worker_ProgressChanged;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            _worker.RunWorkerAsync();
        }

        // ─────────────── أعمال التهيئة (خيط خلفي) ───────────────

        private void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            try
            {
                _worker.ReportProgress(2, "جاري التحقق من قاعدة البيانات...");

                using (var db = new MizanDbContext())
                {
                    // إنشاء قاعدة البيانات وتطبيق الـ Migrations (تفكيك المهام المتزامنة لخيط عامل)
                    db.Database.MigrateAsync().GetAwaiter().GetResult();

                    _worker.ReportProgress(25, "تحميل بيانات الشركة...");

                    // تعبئة البيانات التجريبية عند أول تشغيل فقط
                    if (!db.Users.AnyAsync().GetAwaiter().GetResult())
                        DataSeeder.SeedAsync(db).GetAwaiter().GetResult();
                }

                _worker.ReportProgress(55, "التحقق من حالة الترخيص...");

                // فحص الترخيص: مفعّل / تجربة متبقية / منتهية
                _result.CanRun = ProductStateEngine.Instance.EvaluateCurrentState();

                _worker.ReportProgress(80, "مرحباً بك!");
                System.Threading.Thread.Sleep(350);

                _worker.ReportProgress(100, "مرحباً بك!");
            }
            catch (Exception ex)
            {
                _result.Error = ex;
            }
        }

        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            Progress.Value = e.ProgressPercentage;
            if (e.UserState is string status)
                StatusText.Text = status;
        }

        private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            WorkCompleted?.Invoke(this, _result);
        }

        // ─────────────── التلاشي والإغلاق ───────────────

        /// <summary>يتلاشى خلال 400ms ثم ينفّذ الإجراء المطلوب ويغلق الشاشة.</summary>
        public void BeginFadeOut(Action onFinished)
        {
            if (_fadeStarted)
                return;
            _fadeStarted = true;

            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
            fade.Completed += (_, _) =>
            {
                onFinished();
                Close();
            };

            BeginAnimation(OpacityProperty, fade);
        }
    }
}
