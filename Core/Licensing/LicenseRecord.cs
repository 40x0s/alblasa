using Newtonsoft.Json;

namespace MizanPro.Core.Licensing
{
    /// <summary>
    /// بيانات الترخيص كما تُخزَّن محلياً في الملف: %AppData%\MizanPro\license.json
    /// </summary>
    public sealed class LicenseRecord
    {
        /// <summary>معرّف الجهاز (عنوان MAC) الذي بدأت عليه التجربة أو تم التفعيل عليه.</summary>
        [JsonProperty("machineId")]
        public string MachineId { get; set; } = string.Empty;

        /// <summary>تاريخ بدء الفترة التجريبية.</summary>
        [JsonProperty("trialStartedAt")]
        public DateTime TrialStartedAt { get; set; }

        /// <summary>مدة الفترة التجريبية بالأيام (14 يوماً).</summary>
        [JsonProperty("trialDays")]
        public int TrialDays { get; set; } = 14;

        /// <summary>مفتاح التفعيل المدخل (null ما لم يتم التفعيل).</summary>
        [JsonProperty("activationKey")]
        public string? ActivationKey { get; set; }

        /// <summary>تاريخ التفعيل الناجح.</summary>
        [JsonProperty("activatedAt")]
        public DateTime? ActivatedAt { get; set; }
    }
}
