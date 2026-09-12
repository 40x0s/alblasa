namespace MizanPro.Core.Services
{
    /// <summary>إحصاءات عميل واحد محسوبة من فواتيره.</summary>
    public sealed class CustomerStats
    {
        /// <summary>عدد فواتير العميل (بدون الملغاة).</summary>
        public int TotalInvoices { get; init; }

        /// <summary>إجمالي مبالغ الفواتير (بدون الملغاة).</summary>
        public decimal TotalAmount { get; init; }

        /// <summary>المتبقي المستحق على العميل (فواتير صادرة غير مسددة).</summary>
        public decimal BalanceDue { get; init; }
    }
}
