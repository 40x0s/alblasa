using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MizanPro.Core.Engine;

namespace MizanPro.Controls
{
    /// <summary>
    /// لوحة تفعيل البرنامج: إدخال المفتاح بتنسيق تلقائي والتحقق منه
    /// عبر ProductStateEngine.CommitActivation.
    /// </summary>
    public partial class ActivationPanel : UserControl
    {
        /// <summary>يُطلق عند نجاح التفعيل.</summary>
        public event EventHandler? ActivationSucceeded;

        public ActivationPanel()
        {
            InitializeComponent();
        }

        private async void Activate_Click(object sender, RoutedEventArgs e)
        {
            MessageBorder.Visibility = Visibility.Collapsed;
            ActivateButton.IsEnabled = false;

            try
            {
                var (ok, message) = await ProductStateEngine.Instance.CommitActivation(KeyBox.Text);

                MessageText.Text = message;
                MessageBorder.Background = ok
                    ? (Brush)FindResource("SuccessSoftBrush")
                    : (Brush)FindResource("DangerSoftBrush");
                MessageBorder.BorderBrush = ok
                    ? (Brush)FindResource("SuccessBrush")
                    : (Brush)FindResource("DangerBrush");
                MessageBorder.BorderThickness = new Thickness(1);
                MessageText.Foreground = ok
                    ? (Brush)FindResource("SuccessBrush")
                    : (Brush)FindResource("DangerBrush");
                MessageBorder.Visibility = Visibility.Visible;

                if (ok)
                    ActivationSucceeded?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                ActivateButton.IsEnabled = true;
            }
        }

        /// <summary>تنسيق تلقائي للمفتاح: A-Z و 0-9 فقط، أحرف كبيرة، شرطة كل 5 محارف.</summary>
        private void KeyBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string raw = KeyBox.Text;
            string cleaned = new string(raw.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
            cleaned = new string(cleaned.Where(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9').ToArray());

            if (cleaned.Length > 20)
                cleaned = cleaned[..20];

            string formatted = string.Join("-", Enumerable.Range(0, (cleaned.Length + 4) / 5)
                .Select(i => cleaned.Substring(i * 5, Math.Min(5, cleaned.Length - i * 5))));

            if (raw != formatted)
            {
                int caret = Math.Min(KeyBox.CaretIndex, formatted.Length);
                KeyBox.Text = formatted;
                KeyBox.CaretIndex = Math.Min(caret, formatted.Length);
            }
        }
    }
}
