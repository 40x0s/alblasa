using System.Windows;
using System.Windows.Controls;
using MizanPro.Core.Engine;

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
                var (ok, message) = await ProductStateEngine.Instance.CommitActivation(KeyBox.Text);

                MessageText.Text = message;
                MessageText.Foreground = (Brush)FindResource(ok ? "SuccessBrush" : "DangerBrush");

                if (ok)
                    ActivationSucceeded?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                ActivateButton.IsEnabled = true;
            }
        }
    }
}
