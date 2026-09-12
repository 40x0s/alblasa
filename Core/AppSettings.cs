using Microsoft.Win32;

namespace MizanPro.Core
{
    /// <summary>
    /// مخزن إعدادات خفيف (HKCU\Software\MizanSolutions\Mizan\Settings)
    /// يحفظ بيانات الشركة والتفضيلات واسم المستخدم المُتذكَّر.
    /// </summary>
    public static class AppSettings
    {
        private const string RegistryPath = @"Software\MizanSolutions\Mizan\Settings";

        // أسماء القيم المخزنة
        public const string CompanyName = "CompanyName";
        public const string CompanyAddress = "CompanyAddress";
        public const string CompanyTaxNumber = "CompanyTaxNumber";
        public const string CompanyPhone = "CompanyPhone";
        public const string CompanyEmail = "CompanyEmail";
        public const string VatDefault = "VatDefault";
        public const string InvoicePrefix = "InvoicePrefix";
        public const string RememberUser = "RememberUser";

        /// <summary>قراءة قيمة نصية من الإعدادات (null إذا لم توجد).</summary>
        public static string? Get(string name)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath);
            return key?.GetValue(name) as string;
        }

        /// <summary>كتابة قيمة نصية في الإعدادات.</summary>
        public static void Set(string name, string? value)
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath, writable: true);

            if (string.IsNullOrWhiteSpace(value))
                key.DeleteValue(name, throwOnMissingValue: false);
            else
                key.SetValue(name, value, RegistryValueKind.String);
        }
    }
}
