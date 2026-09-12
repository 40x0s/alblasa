namespace MizanPro.Core.Services
{
    /// <summary>ملخص مالي لشهر معيّن (بدون الفواتير الملغاة).</summary>
    public sealed class MonthlySummary
    {
        public int Year { get; init; }
        public int Month { get; init; }

        /// <summary>عدد الفواتير خلال الشهر (بدون الملغاة).</summary>
        public int InvoiceCount { get; init; }

        /// <summary>إجمالي المبالغ قبل الضريبة وقبل الخصم.</summary>
        public decimal SubTotal { get; init; }

        /// <summary>إجمالي ضريبة القيمة المضافة (15%).</summary>
        public decimal TaxAmount { get; init; }

        /// <summary>إجمالي المبيعات شاملاً الضريبة.</summary>
        public decimal TotalSales { get; init; }

        /// <summary>المبالغ المسددة خلال الشهر.</summary>
        public decimal PaidAmount { get; init; }

        /// <summary>المبالغ المتبقية على العملاء (غير المسددة).</summary>
        public decimal OutstandingAmount { get; init; }
    }
}
