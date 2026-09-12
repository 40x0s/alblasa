using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MizanPro.Core;
using MizanPro.Core.Engine;
using MizanPro.Core.Services;

namespace MizanPro.Windows
{
    /// <summary>
    /// نافذة تسجيل الدخول — تحتوي وضعين:
    ///   A: تسجيل الدخول (مع اهتزاز عند الفشل وإظهار كلمة المرور و"تذكّر جهازي")
    ///   B: تفعيل المنتج (تنسيق تلقائي للمفتاح ورسائل نجاح/خطأ)
    /// </summary>
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();

            SetupLicenseBanner();
            PrefillRememberedUser();

            Loaded += (_, _) => UsernameBox.Focus();
        }

        // ─────────────── حالة الترخيص في أسفل النموذج ───────────────

        private void SetupLicenseBanner()
        {
            if (ProductStateEngine.Plan == "PRO")
            {
                TrialPanel.Visibility = Visibility.Collapsed;
                ProText.Visibility = Visibility.Visible;
            }
            else
            {
                // داخل الفترة التجريبية (وإلا لما وصل المستخدم إلى هنا)
                TrialText.Text = $"⏰ {ProductStateEngine.TrialDaysLeft} أيام متبقية من فترة التجربة";
                TrialPanel.Visibility = Visibility.Visible;
                ProText.Visibility = Visibility.Collapsed;
            }
        }

        private void PrefillRememberedUser()
        {
            string? remembered = AppSettings.Get(AppSettings.RememberUser);
            if (!string.IsNullOrWhiteSpace(remembered))
            {
                UsernameBox.Text = remembered;
                RememberCheck.IsChecked = true;
                Loaded += (_, _) => PasswordBox.Focus();
            }
        }

        // ─────────────── تسجيل الدخول ───────────────

        private async void SignIn_Click(object sender, RoutedEventArgs e)
            => await SignInAsync();

        private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await SignInAsync();
        }

        private void UsernameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                PasswordBox.Focus();
        }

        private async Task SignInAsync()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
            SetSignInLoading(true);

            var (ok, error, _) = await SessionManager.Instance.SignInAsync(
                UsernameBox.Text, PasswordBox.Password);

            if (ok)
            {
                // "تذكّر جهازي": حفظ اسم المستخدم أو إزالته
                if (RememberCheck.IsChecked == true)
                    AppSettings.Set(AppSettings.RememberUser, UsernameBox.Text.Trim());
                else
                    AppSettings.Set(AppSettings.RememberUser, null);

                Close();   // App يفتح MainWindow بعد إغلاق النافذة
                return;
            }

            ErrorText.Text = error ?? "تعذّر تسجيل الدخول.";
            ErrorBorder.Visibility = Visibility.Visible;
            PasswordBox.Clear();
            SetSignInLoading(false);
            ShakeLoginForm();
            PasswordBox.Focus();
        }

        /// <summary>إظهار/إخفاء حالة التحميل لزر الدخول.</summary>
        private void SetSignInLoading(bool loading)
        {
            SignInButton.Visibility = loading ? Visibility.Collapsed : Visibility.Visible;
            SignInProgress.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
            SignInButton.IsEnabled = !loading;
            UsernameBox.IsEnabled = !loading;
            PasswordBox.IsEnabled = !loading;
        }

        /// <summary>اهتزاز نموذج الدخول عند الفشل: 0→14→-14→10→-10→5→-5→0 خلال 500ms.</summary>
        private void ShakeLoginForm()
        {
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(500),
            };

            double[] offsets = { 0, 14, -14, 10, -10, 5, -5, 0 };
            for (int i = 0; i < offsets.Length; i++)
            {
                animation.KeyFrames.Add(new LinearDoubleKeyFrame(
                    offsets[i],
                    KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500.0 * i / (offsets.Length - 1)))));
            }

            LoginTranslate.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        // ─────────────── إظهار / إخفاء كلمة المرور ───────────────

        private void RevealPassword_Click(object sender, RoutedEventArgs e)
        {
            bool reveal = PasswordBox.Visibility == Visibility.Visible;

            if (reveal)
            {
                PasswordRevealBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordRevealBox.Visibility = Visibility.Visible;
                RevealButton.Content = "○";
            }
            else
            {
                PasswordBox.Password = PasswordRevealBox.Text;
                PasswordRevealBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                RevealButton.Content = "●";
            }
        }

        // ─────────────── وضع تفعيل المنتج ───────────────

        private void ShowActivation_Click(object sender, RoutedEventArgs e)
            => ShowActivationPanel();

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
            => ShowLoginPanel();

        private void ShowActivationPanel()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            ActivationPanelHostVisibility(visible: true);
            KeyBox.Focus();
        }

        private void ShowLoginPanel()
        {
            ActivationPanelHostVisibility(visible: false);
            LoginPanel.Visibility = Visibility.Visible;
            SetupLicenseBanner();
            PasswordBox.Focus();
        }

        private void ActivationPanelHostVisibility(bool visible)
            => ActivationPanelView.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>تنسيق تلقائي للمفتاح: حذف غير المسموح، أحرف كبيرة، شرطة كل 5 محارف.</summary>
        private void KeyBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string raw = KeyBox.Text;
            string cleaned = new string(raw.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

            // حذف أي محارف غير لاتينية (نقبل A-Z و 0-9 فقط)
            cleaned = new string(cleaned.Where(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9').ToArray());

            if (cleaned.Length > 20)
                cleaned = cleaned[..20];

            string formatted = string.Join("-", Enumerable.Range(0, (cleaned.Length + 4) / 5)
                .Select(i => cleaned.Substring(i * 5, Math.Min(5, cleaned.Length - i * 5))));

            if (raw != formatted)
            {
                // إعادة بناء النص المنسّق مع الحفاظ على موضع المؤشر في نهاية الكتابة
                int caret = Math.Min(KeyBox.CaretIndex, formatted.Length);
                KeyBox.Text = formatted;
                KeyBox.CaretIndex = Math.Min(caret, formatted.Length);
            }
        }

        private async void KeyBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await ActivateAsync();
        }

        private async void Activate_Click(object sender, RoutedEventArgs e)
            => await ActivateAsync();

        private async Task ActivateAsync()
        {
            ActivationMessageBorder.Visibility = Visibility.Collapsed;
            ActivateButton.Visibility = Visibility.Collapsed;
            ActivateProgress.Visibility = Visibility.Visible;
            ActivateButton.IsEnabled = false;

            try
            {
                var (ok, message) = await ProductStateEngine.Instance.CommitActivation(KeyBox.Text);

                ActivationMessageText.Text = message;
                ActivationMessageBorder.Background = ok
                    ? (Brush)FindResource("SuccessSoftBrush")
                    : (Brush)FindResource("DangerSoftBrush");
                ActivationMessageBorder.BorderBrush = ok
                    ? (Brush)FindResource("SuccessBrush")
                    : (Brush)FindResource("DangerBrush");
                ActivationMessageText.Foreground = ok
                    ? (Brush)FindResource("SuccessBrush")
                    : (Brush)FindResource("DangerBrush");
                ActivationMessageBorder.BorderThickness = new Thickness(1);
                ActivationMessageBorder.Visibility = Visibility.Visible;

                if (ok)
                {
                    // نجاح التفعيل → عودة تلقائية لنموذج الدخول بعد لحظة
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1100) };
                    timer.Tick += (_, _) =>
                    {
                        timer.Stop();
                        ShowLoginPanel();
                    };
                    timer.Start();
                    return;
                }
            }
            finally
            {
                ActivateButton.Visibility = Visibility.Visible;
                ActivateProgress.Visibility = Visibility.Collapsed;
                ActivateButton.IsEnabled = true;
            }
        }

        // ─────────────── النافذة ───────────────

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // نافذة بلا إطار — السحب من أي مساحة فارغة
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); }
                catch { /* تجاهل: الضغط المزدوج أو السحب غير الصالح */ }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
