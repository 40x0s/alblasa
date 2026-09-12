using System.IO;

namespace MizanPro.Core
{
    /// <summary>
    /// المسارات المشتركة للنظام داخل مجلد بيانات المستخدم: %AppData%\MizanPro
    /// </summary>
    public static class MizanPaths
    {
        /// <summary>المجلد الرئيسي لبيانات البرنامج.</summary>
        public static string AppDataFolder { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MizanPro");

        /// <summary>ملف قاعدة البيانات SQLite.</summary>
        public static string DatabaseFilePath => Path.Combine(AppDataFolder, "mizan.db");

        /// <summary>ملف حالة الترخيص المشفّر (AES-128) — يكتبه ProductStateEngine.</summary>
        public static string StateFilePath => Path.Combine(AppDataFolder, "state.enc");

        /// <summary>يضمن وجود مجلد بيانات البرنامج قبل أي عملية كتابة.</summary>
        public static void EnsureAppDataFolder() => Directory.CreateDirectory(AppDataFolder);
    }
}
