using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace MizanPro.Core.Engine
{
    /// <summary>
    /// أدوات معالجة الرموز النصية (Tokens) المستخدمة في النظام.
    /// الاسم عامّ عمداً — انظر تعليق <see cref="ParseAndVerify"/> للتفاصيل الكاملة.
    /// </summary>
    public static class TokenProcessor
    {
        /// <summary>
        /// خوارزمية التحقق من مفتاح الترخيص (شرح تعليمي كامل — مادة الهندسة العكسية)
        /// ═══════════════════════════════════════════════════════════════════════════
        ///
        /// شكل المفتاح: 20 محرفاً من [A-Z0-9] فقط، ويُكتب عادة بصيغة
        /// XXXXX-XXXXX-XXXXX-XXXXX — الشرطات تجميلية تُحذف قبل الفحص،
        /// وحالة الأحرف (كبيرة/صغيرة) لا تؤثر.
        ///
        /// خطوات التحقق:
        ///   1) التنظيف:   clean = rawToken.Replace("-","").ToUpper()
        ///   2) الطول:     clean.Length == 20
        ///   3) الأبجدية:  كل محرف ∈ [A-Z0-9] فقط
        ///   4) المجموع:   sum = مجموع أكواد المحارف (Σ ASCII)
        ///   5) الإقصاء:   xor = XOR تراكمي لكل المحارف (كبايتات)
        ///   6) القبول:    (sum % 73 == 0) && (xor == 0x0A)
        ///
        /// ⚠️ تصحيح رياضي مهم عن المواصفة الأصلية:
        /// اشترطت المواصفة الأصلية (xor == 0x2A) وهذا مستحيل لأي مفتاح طوله 20
        /// من الأبجدية [A-Z0-9]، والبرهان بسيط:
        ///   • الأرقام 0-9  أكوادها 0x30..0x39 → البت 6 = 0 ، البت 5 = 1
        ///   • الأحرف A-Z   أكوادها 0x41..0x5A → البت 6 = 1 ، البت 5 = 0
        ///   • في XOR النهائي: بت6 = زوجية عدد الأحرف ، بت5 = زوجية عدد الأرقام
        ///   • القيمة 0x2A = 00101010 تفرض: بت6=0 (أحرف زوجية) وبت5=1 (أرقام فردية)
        ///     لكن 20 = أحرف + أرقام ⇒ كلاهما زوجي أو كلاهما فردي. تناقض ⇒ لا حل إطلاقاً
        ///     (جرّبنا برمجياً ملايين المفاتيح العشوائية: صفر نتائج — استحالة مُثبَتة).
        /// لذا اعتُمد أصغر تصحيح ممكن: 0x0A (= 0x2A بعد إسقاط البتين المستحيلين 5 و6)
        /// مع الحفاظ على باقي الخوارزمية كما هي حرفياً.
        ///
        /// أمثلة مفاتيح صحيحة (محسوبة ومُتحقَّق منها بالأرقام):
        ///
        ///   1) AAAAABBBBBCCCCCADN89  — من عائلة مثال المواصفة الأصلية:
        ///      المجموع = (65×5) + (66×5) + (67×5) + 65+68+78+56+57
        ///              = 325 + 330 + 335 + 324 = 1314 = 73 × 18 ✓
        ///      XOR     = 0x41^0x42^0x43^0x41^0x44^0x4E^0x38^0x39 = 0x0A ✓
        ///
        ///   2) M1Z4N2025PROM4ST3R01  — مفتاح مقروء لسهولة التدريس:
        ///      المجموع = 1314 = 73 × 18 ✓   و   XOR = 0x0A ✓
        ///      (يُكتب: M1Z4N-2025P-ROM4S-T3R01)
        ///
        /// ملاحظة: مثال المواصفة الأصلية AAAAABBBBBCCCCCDE77A لا يحقق الشرطين:
        ///   المجموع = 1302 و 1302 % 73 = 61 ≠ 0   ،   XOR = 0x00 ≠ 0x0A
        ///
        /// (لطلاب المادة: هذان الشرطان "ضعيفان" عمداً — بمعرفة الخوارزمية
        ///  يمكن اشتقاق مفتاح صالح رياضياً دون كسر أي تشفير.)
        /// </summary>
        public static bool ParseAndVerify(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return false;

            // 1) التنظيف: حذف الشرطات والتوحيد إلى أحرف كبيرة
            string clean = rawToken.Replace("-", "").ToUpperInvariant();

            // 2) الطول يجب أن يكون 20 محرفاً بالضبط (بعد إزالة الشرطات)
            if (clean.Length != 20)
                return false;

            // 3) الأبجدية: لا يُقبل إلا A-Z و 0-9
            foreach (char c in clean)
            {
                bool isUpperLetter = c >= 'A' && c <= 'Z';
                bool isDigit = c >= '0' && c <= '9';

                if (!isUpperLetter && !isDigit)
                    return false;
            }

            // 4) مجموع أكواد المحارف
            int sum = clean.Sum(c => (int)c);

            // 5) XOR تراكمي لكل المحارف كبايتات
            byte xor = 0;
            foreach (char c in clean)
                xor ^= (byte)c;

            // 6) شرطا القبول
            //    (انظر التعليق أعلاه: 0x0A بدل 0x2A المستحيلة رياضياً)
            return sum % 73 == 0 && xor == RequiredXor;
        }

        /// <summary>قيمة XOR المطلوبة في المفتاح الصالح (0x0A = 0b00001010).</summary>
        private const byte RequiredXor = 0x0A;

        /// <summary>
        /// بصمة الجهاز: تجميع اسم الجهاز + اسم المستخدم + أول عنوان MAC
        /// ثم تجزئة الناتج بـ SHA256 وأخذ أول 16 حرفاً سداسياً عشرياً
        /// وتنسيقها: XXXX-XXXX-XXXX-XXXX
        /// بصمة الجهاز تربط ملف الترخيص الاحتياطي (state.enc) بهذا الجهاز تحديداً.
        /// </summary>
        public static string ComputeMachineFingerprint()
        {
            string macAddress = GetFirstMacAddress();

            // الفاصل "|" يمنع التباس الدمج المباشر (AB|C مقابل A|BC)
            string raw = Environment.MachineName + "|" + Environment.UserName + "|" + macAddress;

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));

            // أول 16 حرفاً hex = أول 8 بايتات من التجزئة (حروف كبيرة)
            string hex = Convert.ToHexString(hash, 0, 8);

            return $"{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}";
        }

        /// <summary>
        /// أول عنوان MAC لمحوّل شبكة فعّال (عبر WMI من حزمة System.Management).
        /// يعيد "UNKNOWN-MAC" إذا تعذّر الوصول إلى WMI (صلاحيات محدودة أو بيئة افتراضية).
        /// </summary>
        private static string GetFirstMacAddress()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT MACAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE");

                foreach (ManagementObject adapter in searcher.Get())
                {
                    try
                    {
                        if (adapter["MACAddress"] is string mac && !string.IsNullOrWhiteSpace(mac))
                            return mac.Trim();
                    }
                    finally
                    {
                        adapter.Dispose();
                    }
                }
            }
            catch
            {
                // بيئة غير مدعومة — قيمة بديلة ثابتة
            }

            return "UNKNOWN-MAC";
        }
    }
}
