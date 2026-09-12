using System.Globalization;
using System.Windows.Data;
using MizanPro.Models;

namespace MizanPro.Converters
{
    /// <summary>تحويل نوع الفاتورة (enum) إلى نص عربي للعرض.</summary>
    public sealed class InvoiceTypeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is InvoiceType type
                ? type switch
                {
                    InvoiceType.Sales => "مبيعات",
                    InvoiceType.Purchase => "مشتريات",
                    InvoiceType.Return => "مرتجع",
                    _ => type.ToString(),
                }
                : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException("هذا المحوّل مخصص للعرض فقط.");
    }
}
