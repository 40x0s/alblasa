using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace MizanPro.Core.Engine
{
    /// <summary>
    /// محرك حالة المنتج (Singleton): يحدد هل يعمل البرنامج في وضع التجربة (TRIAL)
    /// أم مفعّلاً بالكامل (PRO)، ويدير تفعيل النسخة.
    ///
    /// مصادر الحقيقة — بالترتيب:
    ///   1) ActivationBlob في الـ Registry (HKCU\Software\MizanSolutions\Mizan\RuntimeState)
    ///      → مفتاح تفعيل صالح ⇒ PRO فوراً. غير صالح ⇒ تُحذف القيمة (عُدِّلت خارجياً).
    ///   2) ملف %AppData%\MizanPro\state.enc (نسخة احتياطية مشفرة AES-128 مرتبطة بالجهاز)
    ///      → محتواه "MIZAN_VALID|بصمة الجهاز" ⇒ يسمح بالتشغيل (مسار احتياطي صامت).
    ///   3) InstallDate في الـ Registry ⇒ حساب أيام التجربة المتبقية (7 أيام).
    /// </summary>
    public sealed class ProductStateEngine
    {
        // ═══════════════ الحالة الداخلية (static كما في المواصفة) ═══════════════

        private static bool _verified = false;

        /// <summary>"TRIAL" أو "PRO".</summary>
        private static string _plan = "TRIAL";

        private static int _trialDaysLeft = 7;

        /// <summary>هل النسخة مفعّلة (آخر تفعيل ناجح)؟</summary>
        public static bool IsVerified => _verified;

        /// <summary>الخطة الحالية: "TRIAL" أو "PRO".</summary>
        public static string Plan => _plan;

        /// <summary>الأيام المتبقية من التجربة (تُحدَّث عند كل فحص حالة).</summary>
        public static int TrialDaysLeft => _trialDaysLeft;

        // ═══════════════ الثوابت ═══════════════

        /// <summary>مدة التجربة المجانية بالأيام.</summary>
        public const int TrialPeriodDays = 7;

        /// <summary>مسار مفتاح الـ Registry الخاص بحالة التشغيل.</summary>
        public const string RegistryPath = @"Software\MizanSolutions\Mizan\RuntimeState";

        private const string ActivationBlobName = "ActivationBlob";
        private const string InstallDateName = "InstallDate";

        /// <summary>العلامة المميزة داخل ملف state.enc بعد فك التشفير.</summary>
        private const string StateMagic = "MIZAN_VALID";

        /// <summary>
        /// مفتاح AES-128 ثابت (16 بايت بالضبط) — ثابتٌ عمداً لأغراض الدورة التعليمية:
        /// المفتاح مضمّن في التجميعة، ومن يفكك البرنامج يستطيع قراءته
        /// وبالتالي فك — أو تزوير — محتوى state.enc (نقطة نقاش أساسية في المادة).
        /// </summary>
        private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("Mizan2024_K3y128");

        /// <summary>متجه تهيئة ثابت (16 بايت) — إعادة استخدام الـ IV ضعف أمني مقصود للدراسة.</summary>
        private static readonly byte[] AesIv = Encoding.UTF8.GetBytes("MizanPro_IV_2024");

        // ═══════════════ Singleton ═══════════════

        /// <summary>المثال الوحيد على مستوى التطبيق.</summary>
        public static ProductStateEngine Instance { get; } = new ProductStateEngine();

        private ProductStateEngine()
        {
        }

        // ═══════════════ الفحص ═══════════════

        /// <summary>
        /// يفحص حالة المنتج ويعيد:
        ///   true  ← يمكن التشغيل (نسخة PRO، أو مسار احتياطي صالح، أو تجربة متبقية)
        ///   false ← التجربة انتهت (يجب إغلاق التطبيق)
        /// </summary>
        public bool EvaluateCurrentState()
        {
            // فتح (أو إنشاء) مفتاح الـ Registry قابلاً للكتابة:
            // قد نحذف ActivationBlob معدَّلاً، أو نكتب InstallDate لأول مرة
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath, writable: true);

            // ─────── الخطوتان 1 + 2: قراءة ActivationBlob ───────
            string? activationBlob = key.GetValue(ActivationBlobName) as string;

            if (!string.IsNullOrEmpty(activationBlob))
            {
                if (TokenProcessor.ParseAndVerify(activationBlob))
                {
                    // مفتاح صالح مخزّن → نسخة PRO
                    _verified = true;
                    _plan = "PRO";
                    return true;
                }

                // القيمة موجودة لكنها غير صالحة → عُدِّلت يدوياً خارج البرنامج → تُحذف
                key.DeleteValue(ActivationBlobName, throwOnMissingValue: false);
            }

            // ─────── الخطوة 3: المسار الاحتياطي state.enc ───────
            if (File.Exists(MizanPaths.StateFilePath))
            {
                try
                {
                    // Base64 ← فك التشفير AES-128 ← نص صريح
                    string plain = DecryptState(File.ReadAllText(MizanPaths.StateFilePath));

                    // المحتوى يجب أن يحتوي العلامة المميزة + بصمة هذا الجهاز تحديداً
                    if (plain.Contains(StateMagic) &&
                        plain.Contains(TokenProcessor.ComputeMachineFingerprint()))
                    {
                        // ملف ترخيص احتياطي صالح على هذا الجهاز → يسمح بالتشغيل.
                        // ملاحظة: هذا المسار "صامت" — لا يرقّي الخطة إلى PRO.
                        return true;
                    }
                }
                catch
                {
                    // ملف تالف أو مشفّر بمفتاح مختلف → يُتجاهل ويستمر الفحص العادي
                }
            }

            // ─────── الخطوة 4: InstallDate ───────
            string? installDateRaw = key.GetValue(InstallDateName) as string;

            if (!DateTime.TryParseExact(
                    installDateRaw,
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime installDate))
            {
                // غير موجودة (أو بصيغة غير صالحة) → تُثبَّت اليوم تاريخاً للتركيب
                installDate = DateTime.Today;
                key.SetValue(InstallDateName, installDate.ToString("yyyyMMdd"), RegistryValueKind.String);
            }

            // ─────── الخطوة 5: حساب أيام التجربة ───────
            int daysSinceInstall = (int)(DateTime.Today - installDate.Date).TotalDays;
            _trialDaysLeft = TrialPeriodDays - daysSinceInstall;

            if (_trialDaysLeft <= 0)
                return false;       // التجربة انتهت

            return true;            // وضع تجربة — أيام متبقية
        }

        // ═══════════════ التفعيل ═══════════════

        /// <summary>
        /// تفعيل النسخة بمفتاح أدخله المستخدم:
        /// يتحقق من المفتاح، ثم يكتب ActivationBlob في الـ Registry
        /// وملف state.enc المشفّر المرتبط بالجهاز، ثم يرقّي الحالة إلى PRO.
        /// </summary>
        public async Task<(bool Ok, string Msg)> CommitActivation(string userInput)
        {
            // التحقق من صلاحية المفتاح أولاً
            if (!TokenProcessor.ParseAndVerify(userInput))
                return (false, "مفتاح الترخيص غير صالح، تحقق من المفتاح وأعد المحاولة");

            // 1) كتابة المفتاح في الـ Registry
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath, writable: true))
                key.SetValue(ActivationBlobName, userInput, RegistryValueKind.String);

            // 2) كتابة ملف state.enc:
            //    "MIZAN_VALID|بصمة الجهاز" ← تشفير AES-128 ← Base64
            string plainText = StateMagic + "|" + TokenProcessor.ComputeMachineFingerprint();
            await File.WriteAllTextAsync(MizanPaths.StateFilePath, EncryptState(plainText));

            // 3) ترقية الحالة في الذاكرة
            _verified = true;
            _plan = "PRO";

            // 4) النتيجة
            return (true, "تم تفعيل ميزان Pro بنجاح!");
        }

        // ═══════════════ تشفير ملف الحالة ═══════════════

        /// <summary>يشفّر نصاً صريحاً بـ AES-128-CBC ويعيده Base64.</summary>
        private static string EncryptState(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = AesKey;
            aes.IV = AesIv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipher = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return Convert.ToBase64String(cipher);
        }

        /// <summary>يفك Base64 ثم يفك التشفير AES-128 ويعيد النص الصريح.</summary>
        private static string DecryptState(string base64)
        {
            using Aes aes = Aes.Create();
            aes.Key = AesKey;
            aes.IV = AesIv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] cipher = Convert.FromBase64String(base64);

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
        }
    }
}
