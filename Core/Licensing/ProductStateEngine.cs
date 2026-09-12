using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace MizanPro.Core.Licensing
{
    /// <summary>
    /// محرك حالة المنتج: يدير الفترة التجريبية المجانية (14 يوماً)
    /// والتفعيل المربوط بالجهاز عبر عنوان MAC.
    ///
    /// آلية التفعيل:
    ///   المفتاح الصحيح = أول 8 بايتات من تجزئة SHA256 لـ (معرّف الجهاز + الملح السري)
    ///   وتُعرض على شكل: XXXX-XXXX-XXXX-XXXX
    /// </summary>
    public static class ProductStateEngine
    {
        /// <summary>مدة الفترة التجريبية المجانية بالأيام.</summary>
        public const int TrialPeriodDays = 14;

        /// <summary>ملح سري يُخلط مع معرّف الجهاز عند توليد المفتاح.</summary>
        private const string ActivationSalt = "MIZAN-PRO-2024-SA";

        /// <summary>
        /// يفحص حالة الترخيص الحالية ويعيد نتيجة تفصيلية تُستخدم لاتخاذ قرار التشغيل:
        ///   - Licensed          → التطبيق يعمل كاملاً
        ///   - TrialActive       → التطبيق يعمل خلال التجربة المجانية
        ///   - TrialExpired      → إغلاق التطبيق
        ///   - ActivationRequired→ عرض لوحة التفعيل داخل نافذة الدخول
        /// </summary>
        public static async Task<ProductStateResult> EvaluateCurrentState()
        {
            string machineId = GetMachineId();

            LicenseRecord? license = await TryLoadLicenseAsync();

            // أول تشغيل على الإطلاق ← إنشاء سجل تجربة جديد لهذا الجهاز
            if (license is null)
            {
                license = new LicenseRecord
                {
                    MachineId = machineId,
                    TrialStartedAt = DateTime.Today,
                    TrialDays = TrialPeriodDays,
                };
                await SaveLicenseAsync(license);
            }

            // الحالة 1: يوجد مفتاح تفعيل مخزّن
            if (!string.IsNullOrEmpty(license.ActivationKey))
            {
                if (string.Equals(license.ActivationKey, GenerateActivationKey(machineId), StringComparison.OrdinalIgnoreCase))
                    return new ProductStateResult
                    {
                        State = ProductState.Licensed,
                        MachineId = machineId,
                        Message = "النسخة مفعّلة — شكراً لثقتكم بميزان برو.",
                    };

                // مفتاح غير صالح لهذا الجهاز → يجب إعادة التفعيل
                return new ProductStateResult
                {
                    State = ProductState.ActivationRequired,
                    MachineId = machineId,
                    Message = "مفتاح التفعيل المخزّن غير صالح لهذا الجهاز — يلزم تفعيل البرنامج.",
                };
            }

            // الحالة 2: ملف الترخيص مرتبط بجهاز آخر (نُقل أو عُدّل يدوياً)
            if (!string.Equals(license.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
                return new ProductStateResult
                {
                    State = ProductState.ActivationRequired,
                    MachineId = machineId,
                    Message = "بيانات الترخيص مرتبطة بجهاز آخر — يلزم تفعيل البرنامج على هذا الجهاز.",
                };

            // الحالة 3: داخل الفترة التجريبية المجانية أو انتهت
            int daysRemaining = (license.TrialStartedAt.Date.AddDays(license.TrialDays) - DateTime.Today).Days;

            if (daysRemaining > 0)
                return new ProductStateResult
                {
                    State = ProductState.TrialActive,
                    TrialDaysRemaining = daysRemaining,
                    MachineId = machineId,
                    Message = $"فترة تجريبية مجانية — متبقٍ {daysRemaining} يوماً من أصل {license.TrialDays}.",
                };

            return new ProductStateResult
            {
                State = ProductState.TrialExpired,
                MachineId = machineId,
                Message = "انتهت الفترة التجريبية المجانية.",
            };
        }

        /// <summary>
        /// تفعيل البرنامج بمفتاح — ينجح فقط إذا كان المفتاح مشتقاً من معرّف هذا الجهاز.
        /// </summary>
        public static async Task<(bool Ok, string? Error)> ActivateAsync(string activationKey)
        {
            string key = (activationKey ?? string.Empty).Trim();

            if (key.Length == 0)
                return (false, "الرجاء إدخال مفتاح التفعيل.");

            string machineId = GetMachineId();

            if (!string.Equals(key, GenerateActivationKey(machineId), StringComparison.OrdinalIgnoreCase))
                return (false, "المفتاح الذي أدخلته غير صالح لهذا الجهاز.");

            // تحميل السجل الحالي، أو إنشاء سجل جديد إذا لم يوجد
            LicenseRecord license = await TryLoadLicenseAsync() ?? new LicenseRecord
            {
                MachineId = machineId,
                TrialStartedAt = DateTime.Today,
                TrialDays = TrialPeriodDays,
            };

            license.MachineId = machineId;
            license.ActivationKey = key.ToUpperInvariant();
            license.ActivatedAt = DateTime.Now;

            await SaveLicenseAsync(license);
            return (true, null);
        }

        // ─────────────── الدوال الداخلية ───────────────

        /// <summary>يولّد مفتاح التفعيل الصحيح لجهاز معيّن.</summary>
        private static string GenerateActivationKey(string machineId)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(machineId + ActivationSalt));

            // أول 8 بايتات على شكل 4 مجموعات سداسية عشرية: XXXX-XXXX-XXXX-XXXX
            return string.Join("-",
                Enumerable.Range(0, 4)
                          .Select(i => hash[i * 2].ToString("X2") + hash[i * 2 + 1].ToString("X2")));
        }

        /// <summary>
        /// معرّف الجهاز = عنوان MAC لأول محول شبكة فعّال (عبر WMI / حزمة System.Management).
        /// </summary>
        private static string GetMachineId()
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
                // بيئات غير مدعومة (صلاحيات محدودة أو أدوات محاكاة) — معرّف بديل ثابت
            }

            return "UNKNOWN-MACHINE";
        }

        /// <summary>يقرأ ملف الترخيص — يعيد null إذا لم يوجد أو كان تالفاً.</summary>
        private static async Task<LicenseRecord?> TryLoadLicenseAsync()
        {
            try
            {
                if (!File.Exists(MizanPaths.LicenseFilePath))
                    return null;

                string json = await File.ReadAllTextAsync(MizanPaths.LicenseFilePath);
                return JsonConvert.DeserializeObject<LicenseRecord>(json);
            }
            catch
            {
                // ملف تالف أو غير قابل للقراءة → يُعامَل كأنه غير موجود
                return null;
            }
        }

        /// <summary>يحفظ ملف الترخيص بصيغة JSON.</summary>
        private static async Task SaveLicenseAsync(LicenseRecord license)
        {
            MizanPaths.EnsureAppDataFolder();

            string json = JsonConvert.SerializeObject(license, Formatting.Indented);
            await File.WriteAllTextAsync(MizanPaths.LicenseFilePath, json);
        }
    }
}
