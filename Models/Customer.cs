namespace MizanPro.Models
{
    /// <summary>عميل (شركة أو مؤسسة أو فرد).</summary>
    public sealed class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>رقم الجوال (مثال: 05XXXXXXXX).</summary>
        public string Phone { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Address { get; set; }

        public string City { get; set; } = string.Empty;

        /// <summary>الرقم الضريبي (VAT) — 15 رقماً إن وُجد.</summary>
        public string? TaxNumber { get; set; }

        /// <summary>الرصيد المستحق على العميل بالريال السعودي.</summary>
        public decimal Balance { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>فواتير هذا العميل.</summary>
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
