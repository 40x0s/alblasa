using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MizanPro.Controls;
using MizanPro.Core.Services;
using MizanPro.Models;

namespace MizanPro.Pages
{
    /// <summary>
    /// لوحة التحكم: مؤشرات شهرية (إيرادات/معلقة/عملاء جدد/مصروفات)
    /// + آخر الفواتير + أبرز العملاء — من خدمات حقيقية.
    /// </summary>
    public partial class DashboardPage : UserControl, IRefreshable
    {
        private bool _loading;

        public DashboardPage()
        {
            InitializeComponent();
            Refresh();
        }

        public void Refresh()
        {
            if (_loading)
                return;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            _loading = true;
            Loader.Visibility = Visibility.Visible;

            try
            {
                var today = DateTime.Now;

                // الترويسة
                var culture = (CultureInfo)CultureInfo.GetCultureInfo("ar-SA").Clone();
                culture.DateTimeFormat.Calendar = new GregorianCalendar();
                HeaderDate.Text = today.ToString("dddd، d MMMM yyyy", culture);

                // ── المؤشرات الشهرية ──
                var summary = await InvoiceService.Instance.GetMonthlySummaryAsync(today.Year, today.Month);

                var prevDate = today.AddMonths(-1);
                var prevSummary = await InvoiceService.Instance.GetMonthlySummaryAsync(prevDate.Year, prevDate.Month);

                KpiRevenueValue.Text = summary.TotalSales.ToString("N2") + " ر.س";

                if (prevSummary.TotalSales > 0)
                {
                    double delta = (double)((summary.TotalSales - prevSummary.TotalSales) / prevSummary.TotalSales) * 100;
                    string arrow = delta >= 0 ? "▲" : "▼";
                    KpiRevenueChange.Text = $"{arrow} {Math.Abs(delta):N1}% عن الشهر السابق";
                    KpiRevenueChange.Foreground = delta >= 0
                        ? (Brush)FindResource("SuccessBrush")
                        : (Brush)FindResource("DangerBrush");
                }
                else
                {
                    KpiRevenueChange.Text = "هذا الشهر";
                    KpiRevenueChange.Foreground = (Brush)FindResource("TextLightBrush");
                }

                // الفواتير المعلقة (صادرة غير مسددة)
                var pending = await InvoiceService.Instance.GetAllAsync(status: InvoiceStatus.Issued);
                KpiPendingValue.Text = pending.Count.ToString("N0");

                // عملاء هذا الشهر
                var customers = await CustomerService.Instance.GetAllAsync();
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var monthEnd = monthStart.AddMonths(1);
                KpiNewCustomersValue.Text = customers
                    .Count(c => c.CreatedAt >= monthStart && c.CreatedAt < monthEnd)
                    .ToString("N0");

                // المصروفات = فواتير المشتريات خلال الشهر
                var monthInvoices = await InvoiceService.Instance.GetAllAsync(from: monthStart, to: monthEnd);
                decimal expenses = monthInvoices
                    .Where(i => i.Type == InvoiceType.Purchase)
                    .Sum(i => i.Total);
                KpiExpensesValue.Text = expenses.ToString("N2") + " ر.س";

                // ── آخر الفواتير ──
                var all = await InvoiceService.Instance.GetAllAsync();
                var recent = all.Take(7).ToList();
                RecentInvoicesGrid.ItemsSource = recent;
                EmptyInvoicesText.Visibility = recent.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                // ── أبرز العملاء (الأعلى قيمة) ──
                var ordered = all
                    .Where(i => i.Customer is not null && i.Status != InvoiceStatus.Cancelled)
                    .GroupBy(i => i.Customer!)
                    .Select(g => (Row: new CustomerRowVm
                    {
                        Name = g.Key.Name,
                        CountText = g.Count() + " فاتورة",
                        AmountText = g.Sum(i => i.Total).ToString("N2") + " ر.س",
                        Initials = AvatarHelper.Initials(g.Key.Name),
                        Brush = AvatarHelper.BrushFor(g.Key.Name),
                    }, Total: g.Sum(i => i.Total)))
                    .OrderByDescending(x => x.Total)
                    .Take(6)
                    .Select(x => x.Row)
                    .ToList();

                TopCustomersList.ItemsSource = ordered;
                EmptyCustomersText.Visibility = ordered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "تعذّر تحميل لوحة التحكم: " + ex.Message,
                    "ميزان برو",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            }
            finally
            {
                Loader.Visibility = Visibility.Collapsed;
                _loading = false;
            }
        }

        /// <summary>صف عرض عميل في قائمة "أبرز العملاء".</summary>
        public sealed class CustomerRowVm
        {
            public string Name { get; set; } = string.Empty;
            public string CountText { get; set; } = string.Empty;
            public string AmountText { get; set; } = string.Empty;
            public string Initials { get; set; } = string.Empty;
            public Brush Brush { get; set; } = Brushes.Gray;
        }
    }
}
