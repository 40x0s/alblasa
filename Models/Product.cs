namespace MizanPro.Models
{
    /// <summary>منتج يُباع ويُشترى داخل الفواتير.</summary>
    public sealed class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>الرقم التعريفي للمنتج Stock Keeping Unit (فريد).</summary>
        public string SKU { get; set; } = string.Empty;

        /// <summary>سعر البيع للعميل (ر.س).</summary>
        public decimal Price { get; set; }

        /// <summary>سعر التكلفة (ر.س).</summary>
        public decimal CostPrice { get; set; }

        /// <summary>الكمية المتوفرة في المخزون.</summary>
        public int Stock { get; set; }

        /// <summary>وحدة القياس (قطعة، علبة، كجم ...).</summary>
        public string Unit { get; set; } = "قطعة";

        public string Category { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>أصناف الفواتير التي تشير إلى هذا المنتج.</summary>
        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    }
}
