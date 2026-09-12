using System.Windows;
using MizanPro.Core.Services;

namespace MizanPro.Windows
{
    /// <summary>
    /// نافذة إضافة منتج جديد: الاسم وSKU إلزاميان، والأسعار والكمية تُتحقق قبل الحفظ.
    /// </summary>
    public partial class ProductDialog : Window
    {
        public ProductDialog()
        {
            InitializeComponent();
            Loaded += (_, _) => NameInput.Focus();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorBorder.Visibility = Visibility.Collapsed;

            // قراءة الأرقام بمرونة (نقطة أو فاصلة عربية)
            decimal price = ParseFlexibleDecimal(PriceInput.Text, out bool priceOk);
            decimal cost = ParseFlexibleDecimal(CostInput.Text, out bool costOk);
            int stock = int.TryParse((StockInput.Text ?? "").Trim(), out int s) ? s : -1;

            if (!priceOk)
            {
                ShowError("سعر البيع يجب أن يكون رقماً صحيحاً.");
                return;
            }

            if (!costOk)
            {
                ShowError("سعر التكلفة يجب أن يكون رقماً صحيحاً.");
                return;
            }

            if (stock < 0)
            {
                ShowError("الكمية الابتدائية يجب أن تكون رقماً صحيحاً غير سالب.");
                return;
            }

            try
            {
                await ProductService.Instance.CreateAsync(
                    name: NameInput.Text,
                    sku: SkuInput.Text,
                    price: price,
                    cost: cost,
                    stock: stock,
                    unit: UnitInput.Text,
                    category: CategoryInput.Text);

                DialogResult = true;
            }
            catch (InvalidOperationException ex)
            {
                ShowError(ex.Message);
            }
            catch (Exception ex)
            {
                ShowError("تعذّر حفظ المنتج: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        /// <summary>قراءة رقم عشري بمرونة — يعيد فشل التحليل عبر المعامل.</summary>
        private static decimal ParseFlexibleDecimal(string text, out bool ok)
        {
            string normalized = (text ?? string.Empty).Trim().Replace('،', '.');
            ok = decimal.TryParse(normalized, out decimal value);
            return ok ? value : 0m;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }
    }
}
