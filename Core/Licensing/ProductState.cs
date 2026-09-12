namespace MizanPro.Core.Licensing
{
    /// <summary>الحالة الحالية للمنتج (تجربة / مفعّل / منتهي / يحتاج تفعيل).</summary>
    public enum ProductState
    {
        /// <summary>النسخة مفعّلة بمفتاح صالح — استخدام كامل بلا قيود.</summary>
        Licensed,

        /// <summary>داخل الفترة التجريبية المجانية.</summary>
        TrialActive,

        /// <summary>انتهت الفترة التجريبية ولم يتم التفعيل — يُغلق التطبيق.</summary>
        TrialExpired,

        /// <summary>يلزم إدخال مفتاح تفعيل صالح (ترخيص غير صالح أو جهاز مختلف).</summary>
        ActivationRequired,
    }

    /// <summary>نتيجة فحص حالة المنتج كما ترجعها الدالة EvaluateCurrentState.</summary>
    public sealed class ProductStateResult
    {
        public ProductState State { get; init; }

        /// <summary>هل النسخة مفعّلة بمفتاح؟</summary>
        public bool IsActivated => State == ProductState.Licensed;

        /// <summary>هل يُسمح للبرنامج بالعمل بهذه الحالة؟ (مفعّل أو داخل التجربة)</summary>
        public bool CanRun => State is ProductState.Licensed or ProductState.TrialActive;

        /// <summary>الأيام المتبقية من الفترة التجريبية (صفر إن لم يكن في التجربة).</summary>
        public int TrialDaysRemaining { get; init; }

        /// <summary>معرّف الجهاز (عنوان MAC) الذي جرى الفحص عليه.</summary>
        public string MachineId { get; init; } = string.Empty;

        /// <summary>رسالة عربية جاهزة للعرض في الواجهة.</summary>
        public string Message { get; init; } = string.Empty;
    }
}
