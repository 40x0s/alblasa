using System.Windows;
using System.Windows.Controls;
using MizanPro.Controls;
using MizanPro.Core.Services;
using MizanPro.Models;
using MizanPro.Windows;

namespace MizanPro.Pages
{
    /// <summary>
    /// صفحة الفواتير: بحث وتصفية وحالات وتواريخ + ترقيم صفحات (20 لكل صفحة)
    /// + إجراءات (عرض/تعليم كمدفوعة/طباعة) + إنشاء فاتورة جديدة عبر نافذة مستقلة.
    /// </summary>
    public partial class InvoicesPage : UserControl, IRefreshable
    {
        private const int PageSize = 20;

        private List<Invoice> _all = new();
        private int _pageIndex = 1;
        private bool _loading;
        private bool _suppressFilters;

        public InvoicesPage()
        {
            InitializeComponent();
        }

        public void Refresh()
        {
            if (_loading)
                return;
            _ = LoadAsync();
        }

        // ─────────────── تحميل البيانات وتصفيتها ───────────────

        private async Task LoadAsync()
        {
            _loading = true;
            Loader.Visibility = Visibility.Visible;

            try
            {
                _all = await InvoiceService.Instance.GetAllAsync(
                    status: ReadStatusFilter(),
                    from: FromPicker.SelectedDate,
                    to: ToPicker.SelectedDate,
                    search: string.IsNullOrWhiteSpace(SearchInput.Text) ? null : SearchInput.Text);

                _pageIndex = 1;
                UpdatePagedView();
            }
            catch (Exception ex)
            {
                ShowError("تعذّر تحميل الفواتير: " + ex.Message);
            }
            finally
            {
                Loader.Visibility = Visibility.Collapsed;
                _loading = false;
            }
        }

        private InvoiceStatus? ReadStatusFilter()
            => StatusCombo.SelectedIndex switch
            {
                1 => InvoiceStatus.Draft,
                2 => InvoiceStatus.Issued,
                3 => InvoiceStatus.Paid,
                4 => InvoiceStatus.Cancelled,
                _ => null,
            };

        private void UpdatePagedView()
        {
            int total = _all.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            _pageIndex = Math.Clamp(_pageIndex, 1, totalPages);

            int skip = (_pageIndex - 1) * PageSize;
            var pageItems = _all.Skip(skip).Take(PageSize).ToList();

            InvoicesGrid.ItemsSource = pageItems;
            EmptyText.Visibility = total == 0 ? Visibility.Visible : Visibility.Collapsed;

            int from = total == 0 ? 0 : skip + 1;
            int to = skip + pageItems.Count;
            PagingText.Text = $"عرض {from}-{to} من {total}";

            FirstPageButton.IsEnabled = _pageIndex > 1;
            PrevPageButton.IsEnabled = _pageIndex > 1;
            NextPageButton.IsEnabled = _pageIndex < totalPages;
            LastPageButton.IsEnabled = _pageIndex < totalPages;
        }

        // ─────────────── أحداث الفلاتر ───────────────

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressFilters)
                return;
            _ = LoadAsync();
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            if (_suppressFilters)
                return;
            _ = LoadAsync();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            _suppressFilters = true;
            SearchInput.Clear();
            StatusCombo.SelectedIndex = 0;
            FromPicker.SelectedDate = null;
            ToPicker.SelectedDate = null;
            _suppressFilters = false;

            _ = LoadAsync();
        }

        /// <summary>يستدعيها شريط البحث العلوي في MainWindow.</summary>
        public void ApplySearch(string term)
        {
            _suppressFilters = true;
            SearchInput.Text = term;
            _suppressFilters = false;
            _ = LoadAsync();
        }

        /// <summary>يستدعيها زر الإشعارات (الجرس) — عرض الفواتير الصادرة (المتأخرة ضمنها).</summary>
        public void ShowIssuedOnly()
        {
            _suppressFilters = true;
            StatusCombo.SelectedIndex = 2;   // صادرة
            _suppressFilters = false;
            _ = LoadAsync();
        }

        // ─────────────── التنقل بين الصفحات ───────────────

        private void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            _pageIndex = 1;
            UpdatePagedView();
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            _pageIndex--;
            UpdatePagedView();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            _pageIndex++;
            UpdatePagedView();
        }

        private void LastPage_Click(object sender, RoutedEventArgs e)
        {
            _pageIndex = int.MaxValue;
            UpdatePagedView();
        }

        // ─────────────── إجراءات الصفوف ───────────────

        private void ViewInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not Invoice invoice)
                return;

            string details =
                "رقم الفاتورة: " + invoice.InvoiceNumber + "\n" +
                "العميل: " + (invoice.Customer?.Name ?? "—") + "\n" +
                "النوع: " + InvoiceTypeText(invoice.Type) + "\n" +
                "الحالة: " + InvoiceStatusText(invoice.Status) + "\n" +
                "تاريخ الإصدار: " + invoice.IssueDate.ToString("yyyy/MM/dd") +
                    (invoice.DueDate is null ? "" : "\nتاريخ الاستحقاق: " + invoice.DueDate.Value.ToString("yyyy/MM/dd")) + "\n" +
                "المجموع قبل الضريبة: " + invoice.SubTotal.ToString("N2") + " ر.س\n" +
                "ضريبة القيمة المضافة 15%: " + invoice.TaxAmount.ToString("N2") + " ر.س\n" +
                "الخصم: " + invoice.Discount.ToString("N2") + " ر.س\n" +
                "الإجمالي: " + invoice.Total.ToString("N2") + " ر.س" +
                    (string.IsNullOrWhiteSpace(invoice.Notes) ? "" : "\n\nملاحظات: " + invoice.Notes);

            MessageBox.Show(details, "تفاصيل الفاتورة " + invoice.InvoiceNumber,
                MessageBoxButton.OK, MessageBoxImage.Information,
                MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
        }

        private async void MarkPaid_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not Invoice invoice)
                return;

            if (invoice.Status != InvoiceStatus.Issued)
            {
                MessageBox.Show(
                    "يمكن تعليم الفواتير الصادرة فقط كمدفوعة.\nحالة الفاتورة الحالية: " + InvoiceStatusText(invoice.Status),
                    "ميزان برو",
                    MessageBoxButton.OK, MessageBoxImage.Information,
                    MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                return;
            }

            var result = MessageBox.Show(
                "هل تريد تعليم الفاتورة " + invoice.InvoiceNumber + " (بمبلغ " + invoice.Total.ToString("N2") + " ر.س) كمدفوعة؟",
                "تأكيد السداد",
                MessageBoxButton.YesNo, MessageBoxImage.Question,
                MessageBoxResult.No, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await InvoiceService.Instance.UpdateStatusAsync(invoice.Id, InvoiceStatus.Paid);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError("تعذّر تحديث الحالة: " + ex.Message);
            }
        }

        private void PrintInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not Invoice invoice)
                return;

            MessageBox.Show(
                "تم إرسال الفاتورة " + invoice.InvoiceNumber + " إلى قائمة الطباعة.\n" +
                "(معاينة الطباعة غير متاحة في هذه النسخة التعليمية)",
                "طباعة",
                MessageBoxButton.OK, MessageBoxImage.Information,
                MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
        }

        // ─────────────── فاتورة جديدة ───────────────

        private void NewInvoice_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NewInvoiceDialog { Owner = Window.GetWindow(this) };

            if (dialog.ShowDialog() == true)
                _ = LoadAsync();
        }

        // ─────────────── مساعدات ───────────────

        private static string InvoiceStatusText(InvoiceStatus status) => status switch
        {
            InvoiceStatus.Draft => "مسودة",
            InvoiceStatus.Issued => "صادرة",
            InvoiceStatus.Paid => "مدفوعة",
            _ => "ملغاة",
        };

        private static string InvoiceTypeText(InvoiceType type) => type switch
        {
            InvoiceType.Sales => "مبيعات",
            InvoiceType.Purchase => "مشتريات",
            _ => "مرتجع",
        };

        private void ShowError(string message)
            => MessageBox.Show(message, "ميزان برو",
                MessageBoxButton.OK, MessageBoxImage.Warning,
                MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
    }
}
