using System.Windows;
using System.Windows.Input;
using MizanPro.Core.Licensing;
using MizanPro.Core.Services;

namespace MizanPro.Windows
{
    /// <summary>
    /// نافذة تسجيل الدخول — تحتوي أيضاً على لوحة التفعيل التي تُعرض تلقائياً
    /// إذا كانت حالة المنتج "يحتاج تفعيل".
    /// </summary>
    public partial class AuthWindow : Window
    {
        private readonly ProductStateResult? _productState;

        /// <param name="needsActivation">
        /// true ← عرض لوحة التفعيل مباشرة بدلاً من نموذج الدخول.
        /// </param>
        /// <param name="productState">نتيجة فحص حالة المنتج (لعرض رسالة التجربة/التفعيل).</param>
        public AuthWindow(bool needsActivation, ProductStateResult? productState = null)
        {
            InitializeComponent();

            _productState = productState;

            // عرض رسالة حالة الترخيص في الشريط الجانبي
            if (!string.IsNullOrWhiteSpace(productState?.Message))
            {
                TrialText.Text = productState.Message;
                TrialBanner.Visibility = Visibility.Visible;
            }
            else
            {
                TrialBanner.Visibility = Visibility.Collapsed;
            }

            if (needsActivation)
                ShowActivationPanel();

            Loaded += (_, _) => UsernameBox.Focus();
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
            TrialText.Text = "تم تفعيل البرنامج بنجاح — يمكنك تسجيل الدخول الآن.";
            TrialBanner.Visibility = Visibility.Visible;
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
