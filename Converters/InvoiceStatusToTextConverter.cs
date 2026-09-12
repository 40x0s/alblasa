using System.Globalization;
using System.Windows.Data;
using MizanPro.Models;

namespace MizanPro.Converters
{
    /// <summary>تحويل حالة الفاتورة (enum) إلى نص عربي للعرض.</summary>
    public sealed class InvoiceStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is InvoiceStatus status
                ? status switch
                {
                    InvoiceStatus.Draft => "مسودة",
                    InvoiceStatus.Issued => "صادرة",
                    InvoiceStatus.Paid => "مسددة",
                    InvoiceStatus.Cancelled => "ملغاة",
                    _ => status.ToString(),
                }
                : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException("هذا المحوّل مخصص للعرض فقط.");
    }
}
