using System.Windows;
using System.Windows.Media;
using MizanPro.Core.Licensing;

namespace MizanPro.Controls
{
    /// <summary>
    /// لوحة تفعيل البرنامج: إدخال المفتاح والتحقق منه عبر ProductStateEngine.
    /// </summary>
    public partial class ActivationPanel : UserControl
    {
        /// <summary>يُطلق عند نجاح التفعيل — ليعيد المستخدم إلى نموذج الدخول.</summary>
        public event EventHandler? ActivationSucceeded;

        public ActivationPanel()
        {
            InitializeComponent();
        }

        private async void Activate_Click(object sender, RoutedEventArgs e)
        {
            MessageText.Text = string.Empty;
            ActivateButton.IsEnabled = false;

            try
            {
                var (ok, error) = await ProductStateEngine.ActivateAsync(KeyBox.Text);

                if (ok)
                {
                    MessageText.Text = "تم تفعيل البرنامج بنجاح — شكراً لك.";
                    MessageText.Foreground = (Brush)FindResource("SuccessBrush");

                    ActivationSucceeded?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    MessageText.Text = error ?? "تعذّر التفعيل.";
                    MessageText.Foreground = (Brush)FindResource("DangerBrush");
                }
            }
            finally
            {
                ActivateButton.IsEnabled = true;
            }
        }
    }
}
