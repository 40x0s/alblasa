using System.Globalization;
using System.Windows.Data;

namespace MizanPro.Converters
{
    /// <summary>تحويل قيمة منطقية (نشط/موقوف) إلى نص عربي للعرض.</summary>
    public sealed class BooleanToStateTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool active
                ? active ? "نشط" : "موقوف"
                : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException("هذا المحوّل مخصص للعرض فقط.");
    }
}
