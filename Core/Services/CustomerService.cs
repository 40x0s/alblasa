using Microsoft.EntityFrameworkCore;
using MizanPro.Data;
using MizanPro.Models;

namespace MizanPro.Core.Services
{
    /// <summary>
    /// خدمة العملاء (Singleton): البحث، الإنشاء، التعديل، الحذف، والإحصاءات.
    /// </summary>
    public sealed class CustomerService
    {
        /// <summary>المثال الوحيد على مستوى التطبيق.</summary>
        public static CustomerService Instance { get; } = new CustomerService();

        private CustomerService()
        {
        }

        /// <summary>
        /// كل العملاء مع إمكانية البحث (بالاسم أو الجوال أو الرقم الضريبي)
        /// والاستثناء الاختياري للعملاء غير النشطين.
        /// </summary>
        public async Task<List<Customer>> GetAllAsync(string? search = null, bool activeOnly = false)
        {
            using var db = new MizanDbContext();

            var query = db.Customers.AsQueryable();

            if (activeOnly)
                query = query.Where(c => c.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(c =>
                    c.Name.Contains(term) ||
                    c.Phone.Contains(term) ||
                    (c.TaxNumber != null && c.TaxNumber.Contains(term)));
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        /// <summary>إنشاء عميل جديد (مع التحقق من صحة الرقم الضريبي 15 رقماً).</summary>
        public async Task<Customer> CreateAsync(
            string name,
            string phone,
            string? email = null,
            string? address = null,
            string? city = null,
            string? taxNumber = null)
        {
            string trimmedName = (name ?? string.Empty).Trim();
            if (trimmedName.Length == 0)
                throw new InvalidOperationException("اسم العميل مطلوب.");

            if (string.IsNullOrWhiteSpace(phone))
                throw new InvalidOperationException("رقم جوال العميل مطلوب.");

            ValidateTaxNumber(taxNumber);

            var customer = new Customer
            {
                Name = trimmedName,
                Phone = phone.Trim(),
                Email = NormalizeOptional(email),
                Address = NormalizeOptional(address),
                City = string.IsNullOrWhiteSpace(city) ? "غير محددة" : city.Trim(),
                TaxNumber = NormalizeOptional(taxNumber),
                Balance = 0m,
                CreatedAt = DateTime.Now,
                IsActive = true,
            };

            using var db = new MizanDbContext();
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            return customer;
        }

        /// <summary>تعديل بيانات عميل — يعيد false إذا لم يوجد.</summary>
        public async Task<bool> UpdateAsync(
            int id,
            string name,
            string phone,
            string? email = null,
            string? address = null,
            string? city = null,
            string? taxNumber = null)
        {
            string trimmedName = (name ?? string.Empty).Trim();
            if (trimmedName.Length == 0)
                throw new InvalidOperationException("اسم العميل مطلوب.");

            if (string.IsNullOrWhiteSpace(phone))
                throw new InvalidOperationException("رقم جوال العميل مطلوب.");

            ValidateTaxNumber(taxNumber);

            using var db = new MizanDbContext();

            var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id);
            if (customer is null)
                return false;

            customer.Name = trimmedName;
            customer.Phone = phone.Trim();
            customer.Email = NormalizeOptional(email);
            customer.Address = NormalizeOptional(address);
            customer.City = string.IsNullOrWhiteSpace(city) ? customer.City : city.Trim();
            customer.TaxNumber = NormalizeOptional(taxNumber);

            await db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// حذف عميل — يعيد false إذا لم يوجد، ويرفض حذف عميل مرتبط بفواتير
        /// (الحل الصحيح: تعطيله بدلاً من حذفه).
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var db = new MizanDbContext();

            var customer = await db.Customers
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer is null)
                return false;

            if (customer.Invoices.Count > 0)
                throw new InvalidOperationException("لا يمكن حذف عميل مرتبط بفواتير — يمكن تعطيله بدلاً من الحذف.");

            db.Customers.Remove(customer);
            await db.SaveChangesAsync();

            return true;
        }

        /// <summary>إحصاءات عميل: عدد الفواتير، إجمالي المبالغ، والمتبقي المستحق.</summary>
        public async Task<CustomerStats> GetStatsAsync(int id)
        {
            using var db = new MizanDbContext();

            bool exists = await db.Customers.AsNoTracking().AnyAsync(c => c.Id == id);
            if (!exists)
                throw new InvalidOperationException("العميل غير موجود.");

            List<Invoice> invoices = await db.Invoices
                .AsNoTracking()
                .Where(i => i.CustomerId == id && i.Status != InvoiceStatus.Cancelled)
                .ToListAsync();

            return new CustomerStats
            {
                TotalInvoices = invoices.Count,
                TotalAmount = invoices.Sum(i => i.Total),
                BalanceDue = invoices
                    .Where(i => i.Status == InvoiceStatus.Issued)
                    .Sum(i => i.Total),
            };
        }

        // ─────────────── الدوال الداخلية ───────────────

        /// <summary>الرقم الضريبي إن وُجد يجب أن يتكوّن من 15 رقماً بالضبط.</summary>
        private static void ValidateTaxNumber(string? taxNumber)
        {
            if (string.IsNullOrWhiteSpace(taxNumber))
                return;

            string trimmed = taxNumber.Trim();
            if (trimmed.Length != 15 || !trimmed.All(char.IsDigit))
                throw new InvalidOperationException("الرقم الضريبي يجب أن يتكوّن من 15 رقماً بالضبط.");
        }

        /// <summary>تحويل النص الاختياري: قيمة فارغة ← null، وإلا النص بدون فراغات زائدة.</summary>
        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
