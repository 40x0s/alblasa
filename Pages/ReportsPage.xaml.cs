using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MizanPro.Controls;
using MizanPro.Core.Services;
using MizanPro.Models;

namespace MizanPro.Pages
{
    /// <summary>
    /// صفحة التقارير: ملخص مالي لشهر محدد (يختاره المستخدم) من InvoiceService
    /// مع توزيع الفواتير حسب الحالة.
    /// </summary>
    public partial class ReportsPage : UserControl, IRefreshable
    {
        private static readonly string[] MonthNames =
        {
            "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر",
        };

        private bool _loading;

        public ReportsPage()
        {
            InitializeComponent();

            int currentMonth = DateTime.Now.Month;
            int currentYear = DateTime.Now.Year;

            foreach (string month in MonthNames)
                MonthCombo.Items.Add(month);
            MonthCombo.SelectedIndex = currentMonth - 1;

            for (int year = currentYear; year >= currentYear - 3; year--)
                YearCombo.Items.Add(year.ToString());
            YearCombo.SelectedIndex = 0;
        }

        public void Refresh()
        {
            if (_loading)
                return;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (MonthCombo.SelectedIndex < 0 || YearCombo.SelectedIndex < 0)
                return;

            _loading = true;
            Loader.Visibility = Visibility.Visible;

            try
            {
                int month = MonthCombo.SelectedIndex + 1;
                int year = int.Parse((string)YearCombo.SelectedItem);

                var summary = await InvoiceService.Instance.GetMonthlySummaryAsync(year, month);

                CountValue.Text = summary.InvoiceCount.ToString("N0");
                SubTotalValue.Text = summary.SubTotal.ToString("N2") + " ر.س";
                VatValue.Text = summary.TaxAmount.ToString("N2") + " ر.س";
                TotalValue.Text = summary.TotalSales.ToString("N2") + " ر.س";
                PaidValue.Text = summary.PaidAmount.ToString("N2") + " ر.س";
                OutstandingValue.Text = summary.OutstandingAmount.ToString("N2") + " ر.س";

                // توزيع الحالات خلال الشهر نفسه
                var start = new DateTime(year, month, 1);
                var end = start.AddMonths(1);
                var invoices = await InvoiceService.Instance.GetAllAsync(from: start, to: end);

                var statusRows = new List<StatusRowVm>
                {
                    new() { Label = "مسودة", Value = CountOf(invoices, InvoiceStatus.Draft), Brush = (Brush)FindResource("TextFaintBrush") },
                    new() { Label = "صادرة", Value = CountOf(invoices, InvoiceStatus.Issued), Brush = (Brush)FindResource("InfoBrush") },
                    new() { Label = "مدفوعة", Value = CountOf(invoices, InvoiceStatus.Paid), Brush = (Brush)FindResource("SuccessBrush") },
                    new() { Label = "ملغاة", Value = CountOf(invoices, InvoiceStatus.Cancelled), Brush = (Brush)FindResource("DangerBrush") },
                };

                StatusList.ItemsSource = statusRows;
                EmptyText.Visibility = summary.InvoiceCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "تعذّر تحميل التقرير: " + ex.Message, "ميزان برو",
                    MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            }
            finally
            {
                Loader.Visibility = Visibility.Collapsed;
                _loading = false;
            }
        }

        private void Period_Changed(object sender, EventArgs e)
        {
            if (_loading)
                return;
            _ = LoadAsync();
        }

        private static string CountOf(List<Invoice> invoices, InvoiceStatus status)
        {
            int count = invoices.Count(i => i.Status == status);
            return count + " فاتورة";
        }

        /// <summary>صف توزيع حالة واحدة.</summary>
        public sealed class StatusRowVm
        {
            public string Label { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
            public Brush Brush { get; set; } = Brushes.Gray;
        }
    }
}
