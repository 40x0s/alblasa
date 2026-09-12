using System.Windows;
using System.Windows.Controls;
using MizanPro.Core.Services;

namespace MizanPro.Windows
{
    /// <summary>
    /// نافذة إضافة عميل جديد: اسم + جوال (إلزاميان) + بريد وعنوان ومدينة ورقم ضريبي (اختيارية).
    /// </summary>
    public partial class CustomerDialog : Window
    {
        public CustomerDialog()
        {
            InitializeComponent();
            Loaded += (_, _) => NameInput.Focus();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorBorder.Visibility = Visibility.Collapsed;

            try
            {
                await CustomerService.Instance.CreateAsync(
                    name: NameInput.Text,
                    phone: PhoneInput.Text,
                    email: EmailInput.Text,
                    address: AddressInput.Text,
                    city: CityInput.Text,
                    taxNumber: TaxNumberInput.Text);

                DialogResult = true;
            }
            catch (InvalidOperationException ex)
            {
                ErrorText.Text = ex.Message;
                ErrorBorder.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ErrorText.Text = "تعذّر حفظ العميل: " + ex.Message;
                ErrorBorder.Visibility = Visibility.Visible;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}
