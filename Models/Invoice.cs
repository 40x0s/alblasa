namespace MizanPro.Models
{
    /// <summary>نوع الفاتورة.</summary>
    public enum InvoiceType
    {
        /// <summary>فاتورة مبيعات — تنقص المخزون.</summary>
        Sales = 0,

        /// <summary>فاتورة مشتريات — تزيد المخزون.</summary>
        Purchase = 1,

        /// <summary>مرتجع مبيعات — يعيد الكمية إلى المخزون.</summary>
        Return = 2,
    }

    /// <summary>حالة الفاتورة في دورة حياتها.</summary>
    public enum InvoiceStatus
    {
        /// <summary>مسودة — لم تُصدر بعد.</summary>
        Draft = 0,

        /// <summary>صادرة — مستحقة السداد.</summary>
        Issued = 1,

        /// <summary>مسددة — تم تحصيل كامل المبلغ.</summary>
        Paid = 2,

        /// <summary>ملغاة.</summary>
        Cancelled = 3,
    }

    /// <summary>فاتورة ضريبية إلكترونية.</summary>
    public sealed class Invoice
    {
        public int Id { get; set; }

        /// <summary>رقم الفاتورة الفريد بالصيغة: INV-2024-00001.</summary>
        public string InvoiceNumber { get; set; } = string.Empty;

        public InvoiceType Type { get; set; } = InvoiceType.Sales;

        /// <summary>العميل صاحب الفاتورة.</summary>
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>المستخدم الذي أنشأ الفاتورة.</summary>
        public int CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        /// <summary>تاريخ إصدار الفاتورة.</summary>
        public DateTime IssueDate { get; set; }

        /// <summary>تاريخ الاستحقاق (السداد) — اختياري.</summary>
        public DateTime? DueDate { get; set; }

        /// <summary>تاريخ السداد الفعلي — يُملأ عند تحويل الحالة إلى "مسددة".</summary>
        public DateTime? PaidAt { get; set; }

        /// <summary>إجمالي الأصناف قبل الضريبة وقبل الخصم.</summary>
        public decimal SubTotal { get; set; }

        /// <summary>ضريبة القيمة المضافة 15% (محسوبة على المبلغ بعد الخصم).</summary>
        public decimal TaxAmount { get; set; }

        /// <summary>الخصم الممنوح (ر.س).</summary>
        public decimal Discount { get; set; }

        /// <summary>الإجمالي النهائي = SubTotal − Discount + TaxAmount.</summary>
        public decimal Total { get; set; }

        public string? Notes { get; set; }

        /// <summary>أصناف (سطور) الفاتورة.</summary>
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }
}
