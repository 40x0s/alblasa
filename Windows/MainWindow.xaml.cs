using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MizanPro.Controls;
using MizanPro.Core.Engine;
using MizanPro.Core.Services;
using MizanPro.Pages;

namespace MizanPro.Windows
{
    /// <summary>
    /// النافذة الرئيسية: شريط علوي داكن + قائمة جانبية متحركة (240↔64 بكسل)
    /// + منطقة صفحات تتلاشى عند التنقل + شريط حالة سفلي.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>الصفحات المُنشأة مسبقاً (تُعاد استعادة كل صفحة عند العودة إليها).</summary>
        private readonly Dictionary<string, UserControl> _pages = new();

        /// <summary>عرض القائمة الجانبية موسّعاً؟</summary>
        private bool _sidebarExpanded = true;

        public MainWindow()
        {
            InitializeComponent();

            SetupUserInfo();
            SelectView("dashboard");

            Loaded += async (_, _) => await LoadBellBadgeAsync();
        }

        // ─────────────── معلومات المستخدم ───────────────

        private void SetupUserInfo()
        {
            var user = SessionManager.Instance.ActiveUser;
            string fullName = user?.FullName ?? "—";
            string role = RoleToArabic(user?.Role ?? Models.UserRole.Viewer);

            AvatarText.Text = AvatarHelper.Initials(fullName);
            SidebarUserName.Text = fullName;
            SidebarUserRole.Text = role;
            StatusUserName.Text = $"{fullName} · {role}";

            // بطاقة الترخيص أسفل القائمة الجانبية
            if (ProductStateEngine.Plan == "PRO")
            {
                LicenseText.Text = "✓ ميزان Pro";
                LicenseText.Foreground = (System.Windows.Media.Brush)FindResource("GoldBrush");
            }
            else
            {
                LicenseText.Text = $"⏰ تجربة · {ProductStateEngine.TrialDaysLeft} أيام";
                LicenseText.Foreground = (System.Windows.Media.Brush)FindResource("WarningBrush");
            }

            StatusDate.Text = FormatArabicDate(DateTime.Now);
        }

        // ─────────────── التنقل بين الصفحات ───────────────

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string key)
                SelectView(key);
        }

        /// <summary>ينشئ الصفحة عند أول طلب، ويستعيرها بعد ذلك، مع تأثير تلاشي 200ms.</summary>
        private void SelectView(string key)
        {
            // إعادة كل أزرار التنقل إلى الوضع العادي
            foreach (var child in NavPanel.Children.OfType<Button>())
                child.Style = (Style)FindResource("NavItemStyle");

            // تمييز الزر المطلوب
            var navButton = NavPanel.Children.OfType<Button>()
                .FirstOrDefault(b => (b.Tag as string) == key);
            if (navButton is not null)
                navButton.Style = (Style)FindResource("NavItemActiveStyle");

            // إنشاء الصفحة عند أول زيارة
            if (!_pages.TryGetValue(key, out UserControl? page) || page is null)
            {
                page = key switch
                {
                    "dashboard" => new DashboardPage(),
                    "invoices" => new InvoicesPage(),
                    "customers" => new CustomersPage(),
                    "products" => new ProductsPage(),
                    "reports" => new ReportsPage(),
                    "settings" => new SettingsPage(),
                    _ => new DashboardPage(),
                };
                _pages[key] = page;
            }

            ContentHost.Content = page;

            // تأثير التلاشي عند التنقل
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            page.Opacity = 0;
            page.BeginAnimation(OpacityProperty, fade);

            // إعادة تحميل بيانات الصفحة
            if (page is IRefreshable refreshable)
                refreshable.Refresh();
        }

        private async Task LoadBellBadgeAsync()
        {
            try
            {
                var overdue = await InvoiceService.Instance.GetOverdueAsync();

                BellBadgeText.Text = overdue.Count.ToString();
                BellBadge.Visibility = overdue.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                BellBadge.ToolTip = $"{overdue.Count} فاتورة متأخرة السداد";
            }
            catch
            {
                BellBadge.Visibility = Visibility.Collapsed;
            }
        }

        // ─────────────── القائمة الجانبية المتحركة ───────────────

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            double from = _sidebarExpanded ? 240 : 64;
            double to = _sidebarExpanded ? 64 : 240;

            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(200),
                DecelerationRatio = 0.6,
            };

            SidebarHost.BeginAnimation(WidthProperty, animation);
            SidebarHost.Width = to;   // القيمة النهائية بعد انتهاء الحركة

            _sidebarExpanded = !_sidebarExpanded;

            if (_sidebarExpanded)
            {
                // التوسيع: النصوص تظهر بعد اكتمال الحركة
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(210),
                };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    SetSidebarLabels(Visibility.Visible);
                };
                timer.Start();
            }
            else
            {
                // التقليص: النصوص تختفي فوراً
                SetSidebarLabels(Visibility.Collapsed);
            }
        }

        /// <summary>إظهار/إخفاء نصوص القائمة الجانبية عند التقليص (تبقى الأيقونات).</summary>
        private void SetSidebarLabels(Visibility visibility)
        {
            NavLabelDashboard.Visibility = visibility;
            NavLabelInvoices.Visibility = visibility;
            NavLabelCustomers.Visibility = visibility;
            NavLabelProducts.Visibility = visibility;
            NavLabelReports.Visibility = visibility;
            NavLabelSettings.Visibility = visibility;
            LicenseText.Visibility = visibility;
            SidebarUserName.Visibility = visibility;
            SidebarUserRole.Visibility = visibility;
            LogoutLabel.Visibility = visibility;
        }

        // ─────────────── الشريط العلوي ───────────────

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            string term = SearchBox.Text.Trim();
            SelectView("invoices");

            if (_pages.TryGetValue("invoices", out var page) && page is InvoicesPage invoices)
                invoices.ApplySearch(term);

            SearchBox.Clear();
            Keyboard.ClearFocus();
        }

        private void Bell_Click(object sender, RoutedEventArgs e)
        {
            // الانتقال إلى الفواتير مع تصفية "صادرة" (المتأخرة ضمنها)
            SelectView("invoices");
            if (_pages.TryGetValue("invoices", out var page) && page is InvoicesPage invoices)
                invoices.ShowIssuedOnly();
        }

        private void Avatar_Click(object sender, RoutedEventArgs e)
            => SelectView("settings");

        // ─────────────── أزرار النافذة ───────────────

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); }
                catch { /* تجاهل */ }
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        /// <summary>
        /// تكبير بحدود منطقة العمل (وليس ملء الشاشة) — يتفادى مشاكل الإطار المخفي
        /// عند التكبير الكامل مع WindowStyle=None.
        /// </summary>
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            var workArea = SystemParameters.WorkArea;

            if (ActualWidth >= workArea.Width - 8 && ActualHeight >= workArea.Height - 8)
            {
                // إرجاع للحجم الافتراضي
                Left = workArea.Left + (workArea.Width - 1280) / 2;
                Top = workArea.Top + (workArea.Height - 800) / 2;
                Width = 1280;
                Height = 800;
            }
            else
            {
                Left = workArea.Left;
                Top = workArea.Top;
                Width = workArea.Width;
                Height = workArea.Height;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
            => Close();

        // ─────────────── تسجيل الخروج ───────────────

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.Instance.SignOut();

            // فتح نافذة دخول جديدة قبل إغلاق الحالية (حتى لا يُغلق التطبيق كلياً)
            var auth = new AuthWindow();
            auth.Closed += (_, _) =>
            {
                if (SessionManager.Instance.IsAuthenticated)
                    new MainWindow().Show();
                else
                    Application.Current.Shutdown();
            };

            auth.Show();
            Close();
        }

        // ─────────────── دوال مساعدة ───────────────

        private static string RoleToArabic(Models.UserRole role) => role switch
        {
            Models.UserRole.Admin => "مدير",
            Models.UserRole.Accountant => "محاسب",
            _ => "مشاهد",
        };

        /// <summary>تاريخ ميلادي بأسماء عربية (بدون التقويم الهجري).</summary>
        private static string FormatArabicDate(DateTime date)
        {
            var culture = (CultureInfo)CultureInfo.GetCultureInfo("ar-SA").Clone();
            culture.DateTimeFormat.Calendar = new GregorianCalendar();
            return date.ToString("dddd، d MMMM yyyy", culture);
        }
    }
}
