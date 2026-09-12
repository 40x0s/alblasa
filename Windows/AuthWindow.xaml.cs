using System.Windows;
using System.Windows.Input;
using MizanPro.Core.Engine;
using MizanPro.Core.Services;

namespace MizanPro.Windows
{
    /// <summary>
    /// نافذة تسجيل الدخول — تحتوي أيضاً على لوحة التفعيل التي تُعرض عند الحاجة
    /// (أو عبر زر "لديّ مفتاح تفعيل").
    /// </summary>
    public partial class AuthWindow : Window
    {
        /// <param name="needsActivation">
        /// true ← عرض لوحة التفعيل مباشرة بدلاً من نموذج الدخول.
        /// </param>
        public AuthWindow(bool needsActivation = false)
        {
            InitializeComponent();

            SetupLicenseBanner();

            if (needsActivation)
                ShowActivationPanel();

            Loaded += (_, _) => UsernameBox.Focus();
        }

        /// <summary>
        /// شريط حالة الترخيص في اللوحة التعريفية:
        /// يعرض الخطة الحالية (PRO) أو الأيام المتبقية من التجربة.
        /// </summary>
        private void SetupLicenseBanner()
        {
            TrialText.Text = ProductStateEngine.Plan == "PRO"
                ? "النسخة مفعّلة — ميزان Pro"
                : $"فترة تجريبية مجانية — متبقٍ {ProductStateEngine.TrialDaysLeft} من {ProductStateEngine.TrialPeriodDays} أيام";

            TrialBanner.Visibility = Visibility.Visible;
        }

        // ─────────────── تسجيل الدخول ───────────────

        private async void SignIn_Click(object sender, RoutedEventArgs e)
        {
            await SignInAsync();
        }

        private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await SignInAsync();
        }

        private void UsernameBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Enter في حقل اسم المستخدم ← الانتقال لكلمة المرور
            if (e.Key == Key.Enter)
                PasswordBox.Focus();
        }

        private async Task SignInAsync()
        {
            ErrorText.Text = string.Empty;
            SignInButton.IsEnabled = false;

            var (ok, error, _) = await SessionManager.Instance.SignInAsync(
                UsernameBox.Text, PasswordBox.Password);

            if (ok)
            {
                // نجح الدخول — إغلاق النافذة (App يفتح MainWindow بعد التحقق من الجلسة)
                Close();
                return;
            }

            ErrorText.Text = error ?? "تعذّر تسجيل الدخول.";
            PasswordBox.Clear();
            SignInButton.IsEnabled = true;
            PasswordBox.Focus();
        }

        // ─────────────── التبديل بين اللوحتين ───────────────

        private void ShowActivation_Click(object sender, RoutedEventArgs e)
            => ShowActivationPanel();

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
            => ShowLoginPanel();

        private void ActivationPanelControl_ActivationSucceeded(object? sender, EventArgs e)
        {
            // بعد التفعيل الناجح: تحديث الشريط ثم العودة لنموذج الدخول
            SetupLicenseBanner();
            ShowLoginPanel();
        }

        private void ShowActivationPanel()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            ActivationHost.Visibility = Visibility.Visible;
        }

        private void ShowLoginPanel()
        {
            ActivationHost.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
        }
    }
}
