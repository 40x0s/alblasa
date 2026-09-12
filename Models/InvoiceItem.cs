namespace MizanPro.Models
{
    /// <summary>صنف واحد (سطر واحد) داخل فاتورة.</summary>
    public sealed class InvoiceItem
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        /// <summary>الكمية المباعة/المشتراة.</summary>
        public decimal Quantity { get; set; }

        /// <summary>سعر الوحدة المتفق عليه في الفاتورة (ر.س).</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>نسبة الضريبة المطبَّقة على السطر (0.15 = 15%).</summary>
        public decimal TaxRate { get; set; } = 0.15m;

        /// <summary>إجمالي السطر قبل الضريبة = Quantity × UnitPrice.</summary>
        public decimal Total { get; set; }
    }
}
