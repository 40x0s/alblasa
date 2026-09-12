using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MizanPro.Core.Services;
using MizanPro.Models;

namespace MizanPro.Windows
{
    /// <summary>
    /// النافذة الرئيسية: شريط جانبي للتنقل + لوحة معلومات وأربع قوائم أساسية.
    /// الشاشات التفصيلية (إنشاء/تعديل الفواتير ...) تُبنى في الأجزاء القادمة من المواصفة.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>ربط أزرار التنقل بلوحاتها وعناوينها.</summary>
        private readonly Dictionary<string, NavView> _views = new();

        public MainWindow()
        {
            InitializeComponent();

            _views["Dashboard"] = new NavView(NavDashboard, DashboardPanel,
                "لوحة المعلومات", "نظرة عامة على أداء النظام خلال الشهر الحالي");
            _views["Invoices"] = new NavView(NavInvoices, InvoicesPanel,
                "الفواتير", "جميع الفواتير المسجلة في النظام");
            _views["Customers"] = new NavView(NavCustomers, CustomersPanel,
                "العملاء", "قائمة العملاء وبياناتهم الاتصالية");
            _views["Products"] = new NavView(NavProducts, ProductsPanel,
                "المنتجات", "قائمة المنتجات وأسعارها ومخزونها");

            // معلومات المستخدم الحالي في الشريط الجانبي والعلوي
            var user = SessionManager.Instance.ActiveUser;
            string roleText = RoleToArabic(user?.Role ?? UserRole.Viewer);
            UserNameText.Text = user?.FullName ?? "—";
            UserRoleText.Text = roleText;
            ChipUserName.Text = user?.Username ?? "—";
            ChipUserRole.Text = "• " + roleText;
            DateText.Text = FormatArabicDate(DateTime.Now);

            SelectView("Dashboard");
        }

        // ─────────────── التنقل ───────────────

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string key && _views.ContainsKey(key))
                SelectView(key);
        }

        private async void SelectView(string key)
        {
            // إخفاء كل اللوحات وإرجاع كل الأزرار للوضع العادي
            foreach (var view in _views.Values)
            {
                view.Button.Style = (Style)FindResource("NavButtonStyle");
                view.Panel.Visibility = Visibility.Collapsed;
            }

            // تمييز القسم المطلوب وإظهار لوحته
            var selected = _views[key];
            selected.Button.Style = (Style)FindResource("NavButtonActiveStyle");
            selected.Panel.Visibility = Visibility.Visible;
            ViewTitle.Text = selected.Title;
            ViewSubtitle.Text = selected.Subtitle;

            try
            {
                await LoadViewDataAsync(key);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "تعذّر تحميل البيانات: " + ex.Message,
                    "ميزان برو",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            }
        }

        /// <summary>يجلب بيانات اللوحة المطلوبة من الخدمات المناسبة.</summary>
        private async Task LoadViewDataAsync(string key)
        {
            switch (key)
            {
                case "Dashboard":
                    await LoadDashboardAsync();
                    break;

                case "Invoices":
                    InvoicesList.ItemsSource = await InvoiceService.Instance.GetAllAsync();
                    break;

                case "Customers":
                    CustomersList.ItemsSource = await CustomerService.Instance.GetAllAsync();
                    break;

                case "Products":
                    ProductsList.ItemsSource = await ProductService.Instance.GetAllAsync();
                    break;
            }
        }

        /// <summary>تعبئة بطاقات الإحصاءات وقائمتي المتأخرات والمخزون المنخفض.</summary>
        private async Task LoadDashboardAsync()
        {
            var today = DateTime.Now;

            var summary = await InvoiceService.Instance.GetMonthlySummaryAsync(today.Year, today.Month);
            var customers = await CustomerService.Instance.GetAllAsync(activeOnly: true);
            var lowStock = await ProductService.Instance.GetLowStockAsync();
            var overdue = await InvoiceService.Instance.GetOverdueAsync();

            MonthInvoicesValue.Text = summary.InvoiceCount.ToString("N0");
            MonthSalesValue.Text = summary.TotalSales.ToString("N2");
            ActiveCustomersValue.Text = customers.Count.ToString("N0");
            LowStockValue.Text = lowStock.Count.ToString("N0");

            OverdueCountText.Text = overdue.Count.ToString();
            OverdueList.ItemsSource = overdue;

            LowStockCountText.Text = lowStock.Count.ToString();
            LowStockList.ItemsSource = lowStock;
        }

        // ─────────────── تسجيل الخروج ───────────────

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.Instance.SignOut();

            // فتح نافذة دخول جديدة، ثم إغلاق النافذة الحالية بعد ظهورها
            // (حتى لا يُغلق التطبيق بالكامل بسبب وضع OnLastWindowClose)
            var auth = new AuthWindow(false);
            auth.Closed += (_, _) =>
            {
                if (SessionManager.Instance.IsAuthenticated)
                {
                    new MainWindow().Show();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            };

            auth.Show();
            Close();
        }

        // ─────────────── دوال مساعدة ───────────────

        /// <summary>اسم الدور بالعربية.</summary>
        private static string RoleToArabic(UserRole role) => role switch
        {
            UserRole.Admin => "مدير النظام",
            UserRole.Accountant => "محاسب",
            _ => "مشاهد",
        };

        /// <summary>تاريخ ميلادي بأسماء عربية (بدون التقويم الهجري).</summary>
        private static string FormatArabicDate(DateTime date)
        {
            var culture = (CultureInfo)CultureInfo.GetCultureInfo("ar-SA").Clone();
            culture.DateTimeFormat.Calendar = new GregorianCalendar();
            return date.ToString("dddd، d MMMM yyyy", culture);
        }

        /// <summary>قسم واحد في التنقل: زر + لوحة + عنوان.</summary>
        private sealed class NavView
        {
            public NavView(Button button, FrameworkElement panel, string title, string subtitle)
            {
                Button = button;
                Panel = panel;
                Title = title;
                Subtitle = subtitle;
            }

            public Button Button { get; }
            public FrameworkElement Panel { get; }
            public string Title { get; }
            public string Subtitle { get; }
        }
    }
}
