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
    /// صفحة العملاء: عرض بطاقات (مع أفاتار وإحصاءات وشريط ديون) أو عرض قائمة جدولي،
    /// مع إضافة عميل جديد عبر نافذة مستقلة.
    /// </summary>
    public partial class CustomersPage : UserControl, IRefreshable
    {
        private bool _loading;

        public CustomersPage()
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
                var customers = await CustomerService.Instance.GetAllAsync();
                var cards = new List<CustomerCardVm>();

                foreach (var customer in customers)
                {
                    var stats = await CustomerService.Instance.GetStatsAsync(customer.Id);
                    cards.Add(new CustomerCardVm
                    {
                        Name = customer.Name,
                        Phone = customer.Phone,
                        Email = string.IsNullOrWhiteSpace(customer.Email) ? "لا يوجد بريد" : customer.Email,
                        Initials = AvatarHelper.Initials(customer.Name),
                        Brush = AvatarHelper.BrushFor(customer.Name),
                        StatsText = $"{stats.TotalInvoices} فاتورة · إجمالي {stats.TotalAmount:N2} ر.س",
                        BalanceText = customer.Balance > 0
                            ? $"مستحقات على العميل: {customer.Balance:N2} ر.س"
                            : "لا توجد مستحقات",
                        BalanceBrush = customer.Balance > 0
                            ? (Brush)FindResource("DangerBrush")
                            : (Brush)FindResource("SuccessBrush"),
                        DebtWidth = 0,   // يُحسب أدناه
                    });
                }

                // شريط الديون: نسبة رصيد العميل إلى أعلى رصيد في القائمة
                decimal maxBalance = Math.Max(1m, customers.Max(c => c.Balance));
                for (int i = 0; i < cards.Count; i++)
                    cards[i].DebtWidth = 220.0 * (double)(customers[i].Balance / maxBalance);

                CardsList.ItemsSource = cards;
                ListGrid.ItemsSource = customers;

                EmptyCardsText.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                EmptyListText.Visibility = cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                SubtitleText.Text = $"{customers.Count} عميلاً مسجلاً في النظام";
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذّر تحميل العملاء: " + ex.Message, "ميزان برو",
                    MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            }
            finally
            {
                Loader.Visibility = Visibility.Collapsed;
                _loading = false;
            }
        }

        // ─────────────── مبدّل العرض ───────────────

        private void CardsToggle_Checked(object sender, RoutedEventArgs e)
        {
            CardsList.Visibility = Visibility.Visible;
            EmptyCardsText.Visibility = CardsList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ListHost.Visibility = Visibility.Collapsed;
            EmptyListText.Visibility = Visibility.Collapsed;
            ListToggle.IsChecked = false;
        }

        private void ListToggle_Checked(object sender, RoutedEventArgs e)
        {
            ListHost.Visibility = Visibility.Visible;
            EmptyListText.Visibility = ListGrid.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            CardsList.Visibility = Visibility.Collapsed;
            EmptyCardsText.Visibility = Visibility.Collapsed;
            CardsToggle.IsChecked = false;
        }

        // ─────────────── عميل جديد ───────────────

        private void NewCustomer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomerDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
                _ = LoadAsync();
        }

        /// <summary>بطاقة عرض عميل واحدة.</summary>
        public sealed class CustomerCardVm
        {
            public string Name { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Initials { get; set; } = string.Empty;
            public Brush Brush { get; set; } = Brushes.Gray;
            public string StatsText { get; set; } = string.Empty;
            public string BalanceText { get; set; } = string.Empty;
            public Brush BalanceBrush { get; set; } = Brushes.Gray;
            public double DebtWidth { get; set; }
        }
    }
}
