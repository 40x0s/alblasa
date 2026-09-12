using Microsoft.EntityFrameworkCore;
using MizanPro.Data;
using MizanPro.Models;

namespace MizanPro.Core.Services
{
    /// <summary>
    /// خدمة الفواتير (Singleton): الإنشاء، البحث والتصفية، تحديث الحالة،
    /// الفواتير المتأخرة، والملخص المالي الشهري.
    /// </summary>
    public sealed class InvoiceService
    {
        /// <summary>نسبة ضريبة القيمة المضافة في المملكة العربية السعودية (15%).</summary>
        public const decimal VatRate = 0.15m;

        /// <summary>المثال الوحيد على مستوى التطبيق.</summary>
        public static InvoiceService Instance { get; } = new InvoiceService();

        private InvoiceService()
        {
        }

        /// <summary>
        /// كل الفواتير مع إمكانية التصفية (الحالة / من تاريخ / إلى تاريخ / بحث بالرقم أو اسم العميل).
        /// </summary>
        public async Task<List<Invoice>> GetAllAsync(
            InvoiceStatus? status = null,
            DateTime? from = null,
            DateTime? to = null,
            string? search = null)
        {
            using var db = new MizanDbContext();

            var query = db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.CreatedBy)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);

            if (from.HasValue)
                query = query.Where(i => i.IssueDate >= from.Value);

            if (to.HasValue)
                // "إلى تاريخ" يشمل اليوم كاملاً (حتى منتصف الليل اليوم التالي)
                query = query.Where(i => i.IssueDate < to.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(i =>
                    i.InvoiceNumber.Contains(term) ||
                    (i.Customer != null && i.Customer.Name.Contains(term)));
            }

            return await query
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();
        }

        /// <summary>
        /// إنشاء فاتورة جديدة:
        ///   - توليد رقم فاتورة تلقائي بالصيغة INV-YYYY-##### (رقم تسلسلي لكل سنة)
        ///   - حساب ضريبة القيمة المضافة 15% لكل سطر
        ///   - خصم الكميات من المخزون (المبيعات تنقص، والمشتريات والمرتجعات تزيد)
        ///   - المسودة (Draft) لا تؤثر على المخزون ولا تاريخ استحقاق لها
        /// </summary>
        /// <param name="status">حالة الفاتورة عند الإنشاء (صادرة افتراضياً — أو مسودة).</param>
        /// <param name="discount">خصم على مستوى الفاتورة (0..SubTotal).</param>
        /// <param name="dueDate">تاريخ الاستحقاق (افتراضياً +30 يوماً إن كانت صادرة).</param>
        public async Task<Invoice> CreateAsync(
            int customerId,
            IReadOnlyList<InvoiceItemInput> items,
            string? notes = null,
            InvoiceType type = InvoiceType.Sales,
            InvoiceStatus status = InvoiceStatus.Issued,
            decimal discount = 0m,
            DateTime? dueDate = null)
        {
            if (items is null || items.Count == 0)
                throw new InvalidOperationException("لا يمكن إنشاء فاتورة بدون أصناف.");

            if (discount < 0)
                throw new InvalidOperationException("الخصم لا يمكن أن يكون سالباً.");

            var createdBy = SessionManager.Instance.ActiveUser
                ?? throw new InvalidOperationException("يجب تسجيل الدخول قبل إنشاء فاتورة.");

            using var db = new MizanDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync();

            var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive)
                ?? throw new InvalidOperationException("العميل المحدد غير موجود أو غير نشط.");

            // ── التحقق من المنتجات والكميات قبل أي حفظ ──
            List<int> productIds = items.Select(i => i.ProductId).Distinct().ToList();
            Dictionary<int, Product> products = await db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var item in items)
            {
                if (item.Quantity <= 0)
                    throw new InvalidOperationException("كمية الصنف يجب أن تكون أكبر من صفر.");

                if (item.UnitPrice < 0)
                    throw new InvalidOperationException("سعر الوحدة لا يمكن أن يكون سالباً.");

                // المخزون يُدار بالوحدات الصحيحة
                if (item.Quantity != Math.Truncate(item.Quantity))
                    throw new InvalidOperationException("الكمية يجب أن تكون رقماً صحيحاً (المخزون يُدار بالوحدات).");

                if (!products.TryGetValue(item.ProductId, out Product? product))
                    throw new InvalidOperationException($"المنتج رقم ({item.ProductId}) غير موجود.");

                if (type == InvoiceType.Sales && product.Stock < item.Quantity)
                    throw new InvalidOperationException(
                        $"الكمية المطلوبة من «{product.Name}» غير متوفرة في المخزون (المتاح: {product.Stock}).");
            }

            // ── بناء الفاتورة ──
            var invoice = new Invoice
            {
                InvoiceNumber = await GenerateNextInvoiceNumberAsync(db),
                Type = type,
                CustomerId = customerId,
                CreatedById = createdBy.Id,
                Status = status,
                IssueDate = DateTime.Now,

                // المسودة لم تُصدر بعد — لا تاريخ استحقاق لها
                DueDate = status == InvoiceStatus.Draft ? null : dueDate ?? DateTime.Now.AddDays(30),

                // تاريخ السداد لا يُسجَّل إلا إذا أُنشئت الفاتورة مسددة مباشرة
                PaidAt = status == InvoiceStatus.Paid ? DateTime.Now : null,

                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                Discount = 0m,
            };

            foreach (var item in items)
            {
                decimal lineTotal = Math.Round(item.Quantity * item.UnitPrice, 2);

                invoice.Items.Add(new InvoiceItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = VatRate,
                    Total = lineTotal,          // إجمالي السطر قبل الضريبة
                });

                invoice.SubTotal += lineTotal;
                invoice.TaxAmount += Math.Round(lineTotal * VatRate, 2);

                // تحديث المخزون حسب نوع الفاتورة — المسودات لا تؤثر على المخزون
                Product product = products[item.ProductId];
                if (status != InvoiceStatus.Draft)
                {
                    int quantity = (int)item.Quantity;
                    if (type == InvoiceType.Sales)
                        product.Stock -= quantity;
                    else
                        product.Stock += quantity;
                }
            }

            // الخصم لا يتجاوز قيمة الأصناف
            if (discount > invoice.SubTotal)
                throw new InvalidOperationException("الخصم لا يمكن أن يتجاوز قيمة الأصناف.");

            invoice.Discount = discount;
            invoice.Total = Math.Round(invoice.SubTotal - invoice.Discount + invoice.TaxAmount, 2);

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return invoice;
        }

        /// <summary>
        /// تحديث حالة فاتورة. الحالات "النشطة" (صادرة/مدفوعة) هي فقط التي تحجز المخزون:
        ///   - الانتقال من نشطة إلى غير نشطة (إلغاء/مسودة) يعيد الكميات إلى المخزون.
        ///   - الانتقال من غير نشطة إلى نشطة (إصدار مسودة) يخصم الكميات مجدداً.
        /// السداد يسجّل تاريخه في PaidAt.
        /// </summary>
        public async Task<bool> UpdateStatusAsync(int invoiceId, InvoiceStatus status)
        {
            using var db = new MizanDbContext();

            var invoice = await db.Invoices
                .Include(i => i.Items)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice is null)
                return false;

            if (invoice.Status == status)
                return true;

            // هل الحالة القديمة والجديدة تختلفان في احتياط المخزون؟
            bool wasActive = invoice.Status is InvoiceStatus.Issued or InvoiceStatus.Paid;
            bool isActive = status is InvoiceStatus.Issued or InvoiceStatus.Paid;

            if (wasActive != isActive)
                ApplyStockMovement(invoice, reverse: !isActive);

            // إصدار مسودة (أو تفعيل ملغاة): إن لم يكن لها استحقاق → 30 يوماً من تاريخ الإصدار
            if (!wasActive && isActive && invoice.DueDate is null)
                invoice.DueDate = invoice.IssueDate.AddDays(30);

            invoice.Status = status;
            invoice.PaidAt = status == InvoiceStatus.Paid ? DateTime.Now : null;

            await db.SaveChangesAsync();
            return true;
        }

        /// <summary>الفواتير الصادرة التي تجاوزت تاريخ استحقاقها دون سداد.</summary>
        public async Task<List<Invoice>> GetOverdueAsync()
        {
            using var db = new MizanDbContext();

            return await db.Invoices
                .Include(i => i.Customer)
                .Where(i => i.Status == InvoiceStatus.Issued && i.DueDate != null && i.DueDate < DateTime.Now)
                .OrderBy(i => i.DueDate)
                .ToListAsync();
        }

        /// <summary>
        /// الملخص المالي لشهر معيّن (بدون الفواتير الملغاة).
        /// الحسابات تُجرى في الذاكرة (وليس في SQL) للحفاظ على دقة النوع decimal.
        /// </summary>
        public async Task<MonthlySummary> GetMonthlySummaryAsync(int year, int month)
        {
            using var db = new MizanDbContext();

            DateTime start = new(year, month, 1);
            DateTime end = start.AddMonths(1);

            List<Invoice> invoices = await db.Invoices
                .Where(i => i.IssueDate >= start && i.IssueDate < end && i.Status != InvoiceStatus.Cancelled)
                .ToListAsync();

            return new MonthlySummary
            {
                Year = year,
                Month = month,
                InvoiceCount = invoices.Count,
                SubTotal = invoices.Sum(i => i.SubTotal),
                TaxAmount = invoices.Sum(i => i.TaxAmount),
                TotalSales = invoices.Sum(i => i.Total),
                PaidAmount = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Total),
                OutstandingAmount = invoices.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.Total),
            };
        }

        // ─────────────── الدوال الداخلية ───────────────

        /// <summary>
        /// يولّد رقم الفاتورة التالي: أعلى رقم تسلسلي في السنة الحالية + 1.
        /// مثال: INV-2026-00013
        /// </summary>
        private static async Task<string> GenerateNextInvoiceNumberAsync(MizanDbContext db)
        {
            int year = DateTime.Now.Year;
            string prefix = $"INV-{year}-";

            string? lastNumber = await db.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastNumber is not null &&
                int.TryParse(lastNumber[prefix.Length..], out int lastSequence))
            {
                sequence = lastSequence + 1;
            }

            return $"{prefix}{sequence:00000}";
        }

        /// <summary>
        /// يطبّق حركة المخزون لكل أصناف الفاتورة، أو يعكسها (عند الإلغاء).
        /// المبيعات تنقص المخزون، والمشتريات والمرتجعات تزيده.
        /// </summary>
        private static void ApplyStockMovement(Invoice invoice, bool reverse)
        {
            foreach (var item in invoice.Items)
            {
                if (item.Product is null)
                    continue;

                int direction = invoice.Type == InvoiceType.Sales ? -1 : 1;
                if (reverse)
                    direction *= -1;

                item.Product.Stock += direction * (int)item.Quantity;
            }
        }
    }
}
