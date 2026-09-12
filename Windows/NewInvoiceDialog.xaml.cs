using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MizanPro.Core.Services;
using MizanPro.Models;

namespace MizanPro.Windows
{
    /// <summary>
    /// نافذة إنشاء فاتورة جديدة:
    /// اختيار العميل والتواريخ + جدول أصناف قابل للتحرير (منتج/كمية/سعر)
    /// مع حساب لحظي للضريبة 15% والإجمالي + خصم وملاحظات.
    /// الإصدار ينشئ فاتورة "صادرة" (تخصم المخزون)، والمسودة لا تؤثر على المخزون.
    /// </summary>
    public partial class NewInvoiceDialog : Window
    {
        /// <summary>مصدر البيانات لجدول الأصناف (يُستخدم من XAML).</summary>
        public ObservableCollection<DraftItem> ItemsSource { get; } = new();

        /// <summary>قائمة المنتجات المتاحة داخل عمود "المنتج".</summary>
        public List<Product> ProductsSource { get; private set; } = new();

        private List<Customer> _customers = new();
        private decimal _discount;

        public NewInvoiceDialog()
        {
            InitializeComponent();
            DataContext = this;

            Loaded += async (_, _) => await LoadAsync();
        }

        // ─────────────── التحميل ───────────────

        private async Task LoadAsync()
        {
            _customers = await CustomerService.Instance.GetAllAsync(activeOnly: true);
            ProductsSource = await ProductService.Instance.GetAllAsync();

            CustomerCombo.ItemsSource = _customers;
            CustomerCombo.DisplayMemberPath = nameof(Customer.Name);

            IssueDateBox.Text = DateTime.Today.ToString("yyyy/MM/dd");
            DueDatePicker.SelectedDate = DateTime.Today.AddDays(30);

            // صف ابتدائي واحد جاهز للتحرير
            if (ItemsSource.Count == 0)
                AddRow(ProductsSource.FirstOrDefault());
        }

        // ─────────────── إدارة الصفوف ───────────────

        private void AddRow(Product? product)
        {
            var item = new DraftItem { Product = product };
            item.PropertyChanged += (_, _) => UpdateSummary();
            ItemsSource.Add(item);
            ItemsGrid.Items.Refresh();
            UpdateSummary();
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
            => AddRow(ProductsSource.FirstOrDefault());

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsGrid.SelectedItem is DraftItem selected)
            {
                ItemsSource.Remove(selected);
                UpdateSummary();
            }
            else
            {
                ShowError("حدّد صفاً من الجدول أولاً.");
            }
        }

        // ─────────────── الملخص المالي ───────────────

        private void DiscountInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            _discount = ParseFlexibleDecimal(DiscountInput.Text);
            UpdateSummary();
        }

        /// <summary>تحديث المجاميع: المجموع + الضريبة 15% − الخصم = الإجمالي.</summary>
        private void UpdateSummary()
        {
            decimal subTotal = ItemsSource.Sum(i => i.Total);
            decimal vat = ItemsSource.Sum(i => i.TaxAmount);
            decimal total = subTotal - _discount + vat;

            SubTotalText.Text = subTotal.ToString("N2") + " ر.س";
            VatText.Text = vat.ToString("N2") + " ر.س";
            TotalText.Text = total.ToString("N2") + " ر.س";
        }

        // ─────────────── الحفظ ───────────────

        private async void Issue_Click(object sender, RoutedEventArgs e)
            => await SaveAsync(InvoiceStatus.Issued);

        private async void Draft_Click(object sender, RoutedEventArgs e)
            => await SaveAsync(InvoiceStatus.Draft);

        private async Task SaveAsync(InvoiceStatus status)
        {
            HideError();

            // التحقق من العميل
            if (CustomerCombo.SelectedItem is not Customer customer)
            {
                ShowError("الرجاء اختيار العميل.");
                return;
            }

            // التحقق من الأصناف
            if (ItemsSource.Count == 0)
            {
                ShowError("أضف صنفاً واحداً على الأقل إلى الفاتورة.");
                return;
            }

            foreach (var item in ItemsSource)
            {
                if (item.Product is null)
                {
                    ShowError("كل صف يجب أن يحتوي على منتج محدد.");
                    return;
                }

                if (item.Quantity <= 0 || item.Quantity != Math.Truncate(item.Quantity))
                {
                    ShowError($"كمية «{item.Product.Name}» يجب أن تكون رقماً صحيحاً أكبر من صفر.");
                    return;
                }

                if (item.UnitPrice < 0)
                {
                    ShowError($"سعر «{item.Product.Name}» لا يمكن أن يكون سالباً.");
                    return;
                }
            }

            decimal subTotal = ItemsSource.Sum(i => i.Total);
            if (_discount < 0 || _discount > subTotal)
            {
                ShowError("الخصم يجب أن يكون بين صفر وقيمة الأصناف (" + subTotal.ToString("N2") + " ر.س).");
                return;
            }

            try
            {
                var inputs = ItemsSource
                    .Select(i => new InvoiceItemInput(i.Product!.Id, i.Quantity, i.UnitPrice))
                    .ToList();

                await InvoiceService.Instance.CreateAsync(
                    customerId: customer.Id,
                    items: inputs,
                    notes: NotesInput.Text,
                    type: InvoiceType.Sales,
                    status: status,
                    discount: _discount,
                    dueDate: DueDatePicker.SelectedDate);

                DialogResult = true;
            }
            catch (InvalidOperationException ex)
            {
                ShowError(ex.Message);
            }
            catch (Exception ex)
            {
                ShowError("تعذّر حفظ الفاتورة: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        // ─────────────── مساعدات ───────────────

        /// <summary>قراءة رقم عشري بمرونة (يقبل النقطة أو الفاصلة العربية كفاصل عشري).</summary>
        private static decimal ParseFlexibleDecimal(string text)
        {
            string normalized = (text ?? string.Empty).Trim().Replace('،', '.');
            return decimal.TryParse(normalized, out decimal value) ? value : 0m;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        private void HideError()
            => ErrorBorder.Visibility = Visibility.Collapsed;

        /// <summary>صنف واحد داخل جدول الفاتورة — يعيد حساب الإجماليات لحظياً.</summary>
        public sealed class DraftItem : INotifyPropertyChanged
        {
            private Product? _product;
            private decimal _quantity = 1m;
            private decimal _unitPrice;

            public Product? Product
            {
                get => _product;
                set
                {
                    _product = value;
                    // سعر البيع الافتراضي عند اختيار المنتج
                    if (value is not null)
                        _unitPrice = value.Price;
                    RaiseAll();
                }
            }

            public decimal Quantity
            {
                get => _quantity;
                set { _quantity = value; RaiseAll(); }
            }

            public decimal UnitPrice
            {
                get => _unitPrice;
                set { _unitPrice = value; RaiseAll(); }
            }

            /// <summary>نسبة الضريبة (15%).</summary>
            public decimal TaxRate => InvoiceService.VatRate;

            /// <summary>ضريبة السطر = 15% من إجمالي السطر.</summary>
            public decimal TaxAmount => Math.Round(Total * TaxRate, 2);

            /// <summary>إجمالي السطر قبل الضريبة.</summary>
            public decimal Total => Math.Round(_quantity * _unitPrice, 2);

            public event PropertyChangedEventHandler? PropertyChanged;

            private void RaiseAll()
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Product)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnitPrice)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TaxAmount)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Total)));
            }
        }
    }
}
