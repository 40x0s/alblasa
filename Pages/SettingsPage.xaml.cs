using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MizanPro.Controls;
using MizanPro.Core;
using MizanPro.Core.Engine;
using MizanPro.Core.Services;

namespace MizanPro.Pages
{
    /// <summary>
    /// صفحة الإعدادات بخمسة تبويبات:
    /// الشركة (Registry) + الحساب (ملف شخصي وكلمة مرور مع شريط قوة)
    /// + الترخيص (حالة التفعيل ومعرّف الجهاز والتفعيل)
    /// + التفضيلات (VAT والبادئة) + عن البرنامج.
    /// </summary>
    public partial class SettingsPage : UserControl, IRefreshable
    {
        public SettingsPage()
        {
            InitializeComponent();
            Refresh();
        }

        public void Refresh()
        {
            LoadCompanyFields();
            LoadProfileFields();
            LoadLicenseState();
            LoadPrefs();
        }

        // ─────────────── تبويب الشركة ───────────────

        private void LoadCompanyFields()
        {
            CompanyNameInput.Text = AppSettings.Get(AppSettings.CompanyName) ?? string.Empty;
            CompanyAddressInput.Text = AppSettings.Get(AppSettings.CompanyAddress) ?? string.Empty;
            CompanyTaxInput.Text = AppSettings.Get(AppSettings.CompanyTaxNumber) ?? string.Empty;
            CompanyPhoneInput.Text = AppSettings.Get(AppSettings.CompanyPhone) ?? string.Empty;
            CompanyEmailInput.Text = AppSettings.Get(AppSettings.CompanyEmail) ?? string.Empty;
        }

        private void SaveCompany_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Set(AppSettings.CompanyName, CompanyNameInput.Text);
            AppSettings.Set(AppSettings.CompanyAddress, CompanyAddressInput.Text);
            AppSettings.Set(AppSettings.CompanyTaxNumber, CompanyTaxInput.Text);
            AppSettings.Set(AppSettings.CompanyPhone, CompanyPhoneInput.Text);
            AppSettings.Set(AppSettings.CompanyEmail, CompanyEmailInput.Text);

            ShowTemporary(CompanySavedText);
        }

        // ─────────────── تبويب الحساب ───────────────

        private void LoadProfileFields()
        {
            var user = SessionManager.Instance.ActiveUser;
            ProfileUsernameText.Text = "الحساب الحالي: " + (user?.Username ?? "—");
            FullNameInput.Text = user?.FullName ?? string.Empty;
            ProfileEmailInput.Text = user?.Email ?? string.Empty;
        }

        private async void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            ProfileSavedText.Visibility = Visibility.Collapsed;

            var (ok, error) = await SessionManager.Instance.UpdateProfileAsync(
                FullNameInput.Text, ProfileEmailInput.Text);

            ProfileSavedText.Text = ok ? "✓ تم حفظ الملف الشخصي" : error;
            ProfileSavedText.Foreground = ok
                ? (Brush)FindResource("SuccessBrush")
                : (Brush)FindResource("DangerBrush");
            ProfileSavedText.Visibility = Visibility.Visible;

            if (ok)
                ShowTemporary(ProfileSavedText, keepText: true);
        }

        private void NewPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
            => UpdateStrengthBar(NewPasswordInput.Password);

        /// <summary>شريط قوة كلمة المرور: 5 مقاطع (ضعيفة → قوية).</summary>
        private void UpdateStrengthBar(string password)
        {
            int score = 0;

            if (password.Length >= 6) score++;
            if (password.Length >= 10) score++;
            if (password.Any(char.IsDigit)) score++;
            if (password.Any(char.IsUpper) && password.Any(char.IsLower)) score++;
            if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

            Brush color = score switch
            {
                0 or 1 => (Brush)FindResource("DangerBrush"),
                2 or 3 => (Brush)FindResource("WarningBrush"),
                _ => (Brush)FindResource("SuccessBrush"),
            };

            var segments = new[] { Seg1, Seg2, Seg3, Seg4, Seg5 };
            for (int i = 0; i < segments.Length; i++)
                segments[i].Background = i < score ? color : (Brush)FindResource("LightBorderBrush");

            StrengthLabel.Text = score switch
            {
                0 => "",
                1 => "قوة كلمة المرور: ضيفة جداً",
                2 => "قوة كلمة المرور: ضيفة",
                3 => "قوة كلمة المرور: متوسطة",
                4 => "قوة كلمة المرور: جيدة",
                _ => "قوة كلمة المرور: قوية",
            };
        }

        private async void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            PasswordChangedText.Visibility = Visibility.Collapsed;

            if (NewPasswordInput.Password.Length < 6)
            {
                ShowPasswordResult("كلمة المرور الجديدة يجب ألا تقل عن 6 أحرف.", ok: false);
                return;
            }

            if (NewPasswordInput.Password != ConfirmPasswordInput.Password)
            {
                ShowPasswordResult("كلمتا المرور الجديدتان غير متطابقتين.", ok: false);
                return;
            }

            bool changed = await SessionManager.Instance.ChangePasswordAsync(
                OldPasswordInput.Password, NewPasswordInput.Password);

            if (changed)
            {
                OldPasswordInput.Clear();
                NewPasswordInput.Clear();
                ConfirmPasswordInput.Clear();
                UpdateStrengthBar(string.Empty);
                ShowPasswordResult("✓ تم تغيير كلمة المرور بنجاح", ok: true);
            }
            else
            {
                ShowPasswordResult("كلمة المرور الحالية غير صحيحة.", ok: false);
            }
        }

        private void ShowPasswordResult(string message, bool ok)
        {
            PasswordChangedText.Text = message;
            PasswordChangedText.Foreground = ok
                ? (Brush)FindResource("SuccessBrush")
                : (Brush)FindResource("DangerBrush");
            PasswordChangedText.Visibility = Visibility.Visible;
        }

        // ─────────────── تبويب الترخيص ───────────────

        private void LoadLicenseState()
        {
            if (ProductStateEngine.Plan == "PRO")
            {
                ProCard.Visibility = Visibility.Visible;
                TrialCard.Visibility = Visibility.Collapsed;
            }
            else
            {
                ProCard.Visibility = Visibility.Collapsed;
                TrialCard.Visibility = Visibility.Visible;
                TrialDaysText.Text = $"⏰ {ProductStateEngine.TrialDaysLeft} من {ProductStateEngine.TrialPeriodDays} أيام متبقية";
            }

            DeviceIdText.Text = TokenProcessor.ComputeMachineFingerprint();
        }

        private void ActivateNow_Click(object sender, RoutedEventArgs e)
            => ActivationPopup.IsOpen = true;

        private void ActivationPanelControl_ActivationSucceeded(object? sender, EventArgs e)
        {
            ActivationPopup.IsOpen = false;
            LoadLicenseState();
        }

        private void CopyDeviceId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(TokenProcessor.ComputeMachineFingerprint());
            }
            catch
            {
                // الحافظة قد تكون مشغولة من عملية أخرى — تجاهل بهدوء
            }
        }

        // ─────────────── تبويب التفضيلات ───────────────

        private void LoadPrefs()
        {
            VatInput.Text = AppSettings.Get(AppSettings.VatDefault) ?? "15";
            PrefixInput.Text = AppSettings.Get(AppSettings.InvoicePrefix) ?? "INV-";
        }

        private void SavePrefs_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Set(AppSettings.VatDefault, VatInput.Text);
            AppSettings.Set(AppSettings.InvoicePrefix, PrefixInput.Text);
            ShowTemporary(PrefsSavedText);
        }

        // ─────────────── مساعدات ───────────────

        /// <summary>يعرض نصاً مؤقتاً ثم يخفيه بعد ثانيتين.</summary>
        private static void ShowTemporary(TextBlock target, bool keepText = false)
        {
            var text = target.Text;
            target.Visibility = Visibility.Visible;

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2),
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                target.Visibility = Visibility.Collapsed;
                if (keepText)
                    target.Text = text;
            };
            timer.Start();
        }
    }
}
