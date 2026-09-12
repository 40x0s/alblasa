using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MizanPro.Controls;
using MizanPro.Core.Services;
using MizanPro.Models;
using MizanPro.Windows;

namespace MizanPro.Pages
{
    /// <summary>
    /// صفحة المنتجات: بطاقات بأيقونات حسب الفئة + بحث وتصفية بالفئة وحالة المخزون.
    /// </summary>
    public partial class ProductsPage : UserControl, IRefreshable
    {
        private bool _loading;

        public ProductsPage()
        {
            InitializeComponent();
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
                string? search = string.IsNullOrWhiteSpace(SearchInput.Text) ? null : SearchInput.Text;

                // بناء قائمة الفئات مع الحفاظ على الاختيار الحالي
                var allProducts = await ProductService.Instance.GetAllAsync();
                var categories = allProducts.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
                string? selectedCategory = CategoryCombo.SelectedItem as string;

                CategoryCombo.Items.Clear();
                CategoryCombo.Items.Add("كل الفئات");
                foreach (var category in categories)
                    CategoryCombo.Items.Add(category);

                CategoryCombo.SelectedItem = categories.Contains(selectedCategory ?? "")
                    ? selectedCategory
                    : "كل الفئات";

                // الفلترة (بحث + فئة) من الخدمة
                string? categoryFilter = CategoryCombo.SelectedItem as string;
                if (categoryFilter == "كل الفئات")
                    categoryFilter = null;

                var products = await ProductService.Instance.GetAllAsync(search, categoryFilter);

                // فلتر حالة المخزون (على العميل)
                products = StockCombo.SelectedIndex switch
                {
                    1 => products.Where(p => p.Stock > 5).ToList(),
                    2 => products.Where(p => p.Stock > 0 && p.Stock <= 5).ToList(),
                    3 => products.Where(p => p.Stock == 0).ToList(),
                    _ => products,
                };

                var cards = products.Select(p => new ProductCardVm
                {
                    Name = p.Name,
                    Sku = p.SKU,
                    PriceText = p.Price.ToString("N2") + " ر.س",
                    StockText = p.Stock switch
                    {
                        0 => "نفد المخزون",
                        <= 5 => $"منخفض — {p.Stock} {p.Unit}",
                        _ => $"متوفر — {p.Stock} {p.Unit}",
                    },
                    StockBrush = p.Stock switch
                    {
                        0 => (Brush)FindResource("DangerBrush"),
                        <= 5 => (Brush)FindResource("WarningBrush"),
                        _ => (Brush)FindResource("SuccessBrush"),
                    },
                    Emoji = EmojiForCategory(p.Category),
                }).ToList();

                CardsList.ItemsSource = cards;
                EmptyText.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                SubtitleText.Text = $"{cards.Count} منتجاً · {products.Count(p => p.Stock <= 5)} منها منخفض المخزون";
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذّر تحميل المنتجات: " + ex.Message, "ميزان برو",
                    MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            }
            finally
            {
                Loader.Visibility = Visibility.Collapsed;
                _loading = false;
            }
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_loading)
                return;
            _ = LoadAsync();
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            if (_loading)
                return;
            _ = LoadAsync();
        }

        private void NewProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
                _ = LoadAsync();
        }

        /// <summary>أيقونة تعبيرية تعبّر عن فئة المنتج.</summary>
        private static string EmojiForCategory(string category) => category switch
        {
            "أجهزة" => "💻",
            "طابعات" => "🖨️",
            "خوادم" => "🗄️",
            "شاشات" => "🖥️",
            "ملحقات" => "⌨️",
            "تخزين" => "💾",
            "مكونات" => "🔧",
            "شبكات" => "🌐",
            "مراقبة" => "📹",
            "عرض" => "📽️",
            "طاقة" => "🔋",
            _ => "📦",
        };

        /// <summary>بطاقة عرض منتج واحدة.</summary>
        public sealed class ProductCardVm
        {
            public string Name { get; set; } = string.Empty;
            public string Sku { get; set; } = string.Empty;
            public string PriceText { get; set; } = string.Empty;
            public string StockText { get; set; } = string.Empty;
            public Brush StockBrush { get; set; } = Brushes.Gray;
            public string Emoji { get; set; } = "📦";
        }
    }
}
