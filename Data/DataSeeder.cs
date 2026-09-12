using Microsoft.EntityFrameworkCore;
using MizanPro.Models;

namespace MizanPro.Data
{
    /// <summary>
    /// تعبئة قاعدة البيانات ببيانات تجريبية واقعية عند أول تشغيل للبرنامج فقط.
    /// </summary>
    public static class DataSeeder
    {
        /// <summary>
        /// يعبّئ البيانات التجريبية: مستخدمان + 15 عميلاً + 20 منتجاً + 12 فاتورة.
        /// تعمل مرة واحدة فقط (تتوقف فوراً إذا وُجد مستخدمون مسبقاً).
        /// </summary>
        public static async Task SeedAsync(MizanDbContext db)
        {
            // حماية إضافية: لا تعبّئ إذا كانت قاعدة البيانات جاهزة مسبقاً
            if (await db.Users.AnyAsync())
                return;

            // ═══════════════ 1) المستخدمون ═══════════════
            var admin = new User
            {
                Username = "admin",
                Email = "admin@mizanpro.sa",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FullName = "مدير النظام",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-200),
            };

            var accountant = new User
            {
                Username = "accountant",
                Email = "accountant@mizanpro.sa",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Acc@2024"),
                FullName = "المحاسب العام",
                Role = UserRole.Accountant,
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-199),
            };

            db.Users.AddRange(admin, accountant);

            // ═══════════════ 2) العملاء (15 عميلاً بأسماء سعودية) ═══════════════
            Customer[] customers =
            {
                new() { Name = "شركة الوطنية للتجارة",     Phone = "0551234567", Email = "info@watania.sa",      Address = "طريق الملك فهد، حي العليا",      City = "الرياض",       TaxNumber = "310123456700003", CreatedAt = DateTime.Now.AddDays(-190) },
                new() { Name = "مؤسسة النور للتوريدات",   Phone = "0562345678", Email = "sales@alnoor.sa",      Address = "شارع التحلية، حي الروضة",        City = "جدة",          TaxNumber = "310234567800006", CreatedAt = DateTime.Now.AddDays(-185) },
                new() { Name = "مجموعة الخليج القابضة",   Phone = "0503456789", Email = "contact@gulfgroup.sa", Address = "شارع الملك سعود، حي الشاطئ",     City = "الدمام",       TaxNumber = "310345678900009", CreatedAt = DateTime.Now.AddDays(-180) },
                new() { Name = "مؤسسة الرياض للمقاولات",  Phone = "0544567890", Email = "info@riyadh-est.sa",   Address = "طريق الخرج، حي النسيم",           City = "الرياض",       TaxNumber = "310456789000002", CreatedAt = DateTime.Now.AddDays(-170) },
                new() { Name = "شركة الصحراء للتقنية",    Phone = "0555678901", Email = "info@sahara-tech.sa",  Address = "شارع الأمير فيصل بن فهد",         City = "الخبر",        TaxNumber = "310567890100005", CreatedAt = DateTime.Now.AddDays(-160) },
                new() { Name = "مؤسسة الوفاء التجارية",   Phone = "0566789012", Email = "info@wafa.sa",         Address = "حي العزيزية",                     City = "مكة المكرمة",  TaxNumber = "310678901200008", CreatedAt = DateTime.Now.AddDays(-150) },
                new() { Name = "شركة الأفق الجديد",       Phone = "0507890123", Email = "info@ofuq.sa",         Address = "طريق المدينة المنورة",            City = "المدينة المنورة", TaxNumber = "310789012300001", CreatedAt = DateTime.Now.AddDays(-140) },
                new() { Name = "مؤسسة البركة للأثاث المكتبي", Phone = "0548901234", Email = "info@baraka.sa",   Address = "شارع التخصصي",                    City = "الرياض",       TaxNumber = "310890123400004", CreatedAt = DateTime.Now.AddDays(-130) },
                new() { Name = "شركة نجد للخدمات اللوجستية", Phone = "0559012345", Email = "info@najd-log.sa", Address = "حي الصناعية",                     City = "بريدة",        TaxNumber = "310901234500007", CreatedAt = DateTime.Now.AddDays(-120) },
                new() { Name = "مؤسسة قرطبة للتجارة",     Phone = "0560123456", Email = "info@qurtuba.sa",      Address = "شارع الأندلس، حي الشرفية",        City = "جدة",          TaxNumber = "310112345600010", CreatedAt = DateTime.Now.AddDays(-110) },
                new() { Name = "شركة الوسام للتوريدات",   Phone = "0502234567", Email = "info@wisam.sa",        Address = "شارع الملك عبدالعزيز",            City = "أبها",         TaxNumber = null,              CreatedAt = DateTime.Now.AddDays(-100) },
                new() { Name = "مؤسسة المدينة للصيانة",   Phone = "0543345678", Email = null,                   Address = "حي الحرم",                        City = "المدينة المنورة", TaxNumber = null,          CreatedAt = DateTime.Now.AddDays(-90) },
                new() { Name = "شركة ساس للحلول البرمجية", Phone = "0554456789", Email = "info@sas-soft.sa",    Address = "حي الياسمين",                     City = "الرياض",       TaxNumber = "310445678900011", CreatedAt = DateTime.Now.AddDays(-80) },
                new() { Name = "مؤسسة الياسمين للتجارة",  Phone = "0565567890", Email = null,                   Address = "حي شهار",                         City = "الطائف",       TaxNumber = null,              CreatedAt = DateTime.Now.AddDays(-70) },
                new() { Name = "مجموعة الفهد التجارية",   Phone = "0506678901", Email = "info@fahd-group.sa",   Address = "حي الفيصلية",                     City = "الدمام",       TaxNumber = "310667890100012", CreatedAt = DateTime.Now.AddDays(-60) },
            };

            db.Customers.AddRange(customers);

            // ═══════════════ 3) المنتجات (20 منتجاً بأسعار منطقية بالريال) ═══════════════
            Product[] products =
            {
                new() { Name = "كمبيوتر محمول Dell Latitude 5540",        SKU = "LAP-001", Price = 4899m,  CostPrice = 4150m,  Stock = 12, Unit = "قطعة", Category = "أجهزة" },
                new() { Name = "كمبيوتر مكتبي HP ProDesk 400 G9",         SKU = "DES-001", Price = 2799m,  CostPrice = 2400m,  Stock = 8,  Unit = "قطعة", Category = "أجهزة" },
                new() { Name = "طابعة ليزر HP LaserJet Pro M404",          SKU = "PRT-001", Price = 1149m,  CostPrice = 950m,   Stock = 15, Unit = "قطعة", Category = "طابعات" },
                new() { Name = "طابعة حرارية للفواتير Epson TM-T20",      SKU = "PRT-002", Price = 689m,   CostPrice = 520m,   Stock = 20, Unit = "قطعة", Category = "طابعات" },
                new() { Name = "ماكينة نسخ Canon imageRUNNER 2625",       SKU = "CPY-001", Price = 8999m,  CostPrice = 7600m,  Stock = 3,  Unit = "قطعة", Category = "أجهزة" },
                new() { Name = "خادم Dell PowerEdge T350",                 SKU = "SRV-001", Price = 14500m, CostPrice = 12300m, Stock = 2,  Unit = "قطعة", Category = "خوادم" },
                new() { Name = "شاشة LG مقاس 27 بوصة IPS",                SKU = "MON-001", Price = 1099m,  CostPrice = 880m,   Stock = 18, Unit = "قطعة", Category = "شاشات" },
                new() { Name = "شاشة Dell مقاس 24 بوصة",                  SKU = "MON-002", Price = 749m,   CostPrice = 610m,   Stock = 25, Unit = "قطعة", Category = "شاشات" },
                new() { Name = "لوحة مفاتيح لاسلكية Logitech K345",       SKU = "KBD-001", Price = 149m,   CostPrice = 105m,   Stock = 40, Unit = "قطعة", Category = "ملحقات" },
                new() { Name = "فأرة لاسلكية Logitech M220",              SKU = "MSE-001", Price = 89m,    CostPrice = 55m,    Stock = 60, Unit = "قطعة", Category = "ملحقات" },
                new() { Name = "سماعة رأس Jabra Evolve 20",               SKU = "HDN-001", Price = 349m,   CostPrice = 265m,   Stock = 14, Unit = "قطعة", Category = "ملحقات" },
                new() { Name = "قرص SSD Samsung 870 EVO بسعة 1TB",        SKU = "SSD-001", Price = 385m,   CostPrice = 300m,   Stock = 35, Unit = "قطعة", Category = "تخزين" },
                new() { Name = "قرص صلب خارجي WD بسعة 2TB",               SKU = "HDD-001", Price = 299m,   CostPrice = 230m,   Stock = 22, Unit = "قطعة", Category = "تخزين" },
                new() { Name = "ذاكرة RAM DDR4 بسعة 16GB",                SKU = "RAM-001", Price = 215m,   CostPrice = 165m,   Stock = 30, Unit = "قطعة", Category = "مكونات" },
                new() { Name = "راوتر TP-Link Archer AX23",               SKU = "RTR-001", Price = 429m,   CostPrice = 340m,   Stock = 16, Unit = "قطعة", Category = "شبكات" },
                new() { Name = "سويتش شبكة D-Link بـ 24 منفذاً",          SKU = "SWI-001", Price = 1350m,  CostPrice = 1100m,  Stock = 5,  Unit = "قطعة", Category = "شبكات" },
                new() { Name = "كاميرا مراقبة Hikvision بدقة 4MP",        SKU = "CAM-001", Price = 325m,   CostPrice = 240m,   Stock = 28, Unit = "قطعة", Category = "مراقبة" },
                new() { Name = "جهاز عرض Epson EB-X51",                   SKU = "PRJ-001", Price = 2890m,  CostPrice = 2450m,  Stock = 4,  Unit = "قطعة", Category = "عرض" },
                new() { Name = "مزود طاقة UPS APC بقدرة 1500VA",          SKU = "UPS-001", Price = 1150m,  CostPrice = 940m,   Stock = 6,  Unit = "قطعة", Category = "طاقة" },
                new() { Name = "قارئ باركود Zebra DS2208",                SKU = "BAR-001", Price = 315m,   CostPrice = 250m,   Stock = 3,  Unit = "قطعة", Category = "ملحقات" },
            };

            db.Products.AddRange(products);

            // حفظ أولي للحصول على المعرّفات (Id) قبل إنشاء الفواتير
            await db.SaveChangesAsync();

            // ═══════════════ 4) الفواتير (12 فاتورة بحالات وتواريخ متنوعة) ═══════════════
            // المعرّفات: CustomerIndex و ProductIndex تشير إلى مواقع العناصر في القائمتين أعلاه.
            SeedInvoice[] specs =
            {
                new(0,  170, InvoiceStatus.Paid,      3, 0, new (int, decimal)[] { (0, 2m), (6, 2m) }),      // كمبيوتران محمولان + شاشتان
                new(1,  150, InvoiceStatus.Paid,      5, 1, new (int, decimal)[] { (2, 3m) }),               // 3 طابعات ليزر
                new(2,  132, InvoiceStatus.Paid,      2, 0, new (int, decimal)[] { (5, 1m), (15, 1m) }),     // خادم + سويتش
                new(3,  118, InvoiceStatus.Issued,    0, 1, new (int, decimal)[] { (8, 10m), (9, 10m) }),    // لوحات مفاتيح + فأرات — متأخرة
                new(4,  103, InvoiceStatus.Paid,      7, 0, new (int, decimal)[] { (17, 1m) }),              // جهاز عرض
                new(5,   92, InvoiceStatus.Issued,    0, 1, new (int, decimal)[] { (11, 5m), (13, 5m) }),    // أقراص SSD + ذواكر — متأخرة
                new(6,   80, InvoiceStatus.Cancelled, 0, 0, new (int, decimal)[] { (16, 4m) }),              // كاميرات — ملغاة
                new(7,   66, InvoiceStatus.Paid,      4, 1, new (int, decimal)[] { (18, 2m) }),              // مزودا طاقة
                new(8,   50, InvoiceStatus.Issued,    0, 0, new (int, decimal)[] { (0, 1m), (10, 1m) }),     // كمبيوتر محمول + سماعة — متأخرة
                new(9,   34, InvoiceStatus.Draft,     0, 1, new (int, decimal)[] { (3, 2m), (19, 1m) }),     // طابعة حرارية + قارئ باركود — مسودة
                new(10,  19, InvoiceStatus.Issued,    0, 0, new (int, decimal)[] { (7, 3m) }),               // 3 شاشات Dell
                new(1,    8, InvoiceStatus.Paid,      2, 1, new (int, decimal)[] { (12, 4m), (14, 2m) }),    // أقراص خارجية + راوتران
            };

            var invoices = new List<Invoice>();

            // عدّاد تسلسلي لكل سنة على حدة (INV-2025-00001، INV-2026-00001 ...)
            var yearCounters = new Dictionary<int, int>();

            // الأقدم أولاً حتى تكون أرقام الفواتير تصاعدية زمنياً
            foreach (var spec in specs.OrderByDescending(s => s.DaysAgo))
            {
                DateTime issueDate = DateTime.Today.AddDays(-spec.DaysAgo).AddHours(10 + invoices.Count % 8);

                yearCounters.TryGetValue(issueDate.Year, out int sequence);
                yearCounters[issueDate.Year] = sequence + 1;

                invoices.Add(BuildInvoice(
                    number: $"INV-{issueDate.Year}-{sequence + 1:00000}",
                    customer: customers[spec.CustomerIndex],
                    createdBy: spec.CreatedByIndex == 0 ? admin : accountant,
                    issueDate: issueDate,
                    status: spec.Status,
                    paidAfterDays: spec.PaidAfterDays,
                    items: spec.Items.Select(i => (products[i.ProductIndex], i.Quantity)).ToArray()));
            }

            db.Invoices.AddRange(invoices);
            await db.SaveChangesAsync();

            // ═══════════════ 5) أرصدة العملاء ═══════════════
            // الرصيد = مجموع الفواتير الصادرة (غير المسددة وغير الملغاة) لكل عميل
            foreach (var customer in customers)
            {
                customer.Balance = invoices
                    .Where(i => i.CustomerId == customer.Id && i.Status == InvoiceStatus.Issued)
                    .Sum(i => i.Total);
            }

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// يبني فاتورة مبيعات كاملة الحسابات (ضريبة 15% وخصم صفر) من مواصفة بذور.
        /// </summary>
        private static Invoice BuildInvoice(
            string number,
            Customer customer,
            User createdBy,
            DateTime issueDate,
            InvoiceStatus status,
            int paidAfterDays,
            IEnumerable<(Product Product, decimal Quantity)> items)
        {
            var invoice = new Invoice
            {
                InvoiceNumber = number,
                Type = InvoiceType.Sales,
                CustomerId = customer.Id,
                CreatedById = createdBy.Id,
                Status = status,
                IssueDate = issueDate,

                // المسودات لم تُصدر بعد — لا تاريخ استحقاق لها
                DueDate = status == InvoiceStatus.Draft ? null : issueDate.AddDays(30),

                // المسددة فقط لها تاريخ سداد
                PaidAt = status == InvoiceStatus.Paid ? issueDate.AddDays(paidAfterDays) : null,

                Discount = 0m,
                Notes = null,
            };

            foreach (var (product, quantity) in items)
            {
                decimal lineTotal = Math.Round(quantity * product.Price, 2);

                invoice.Items.Add(new InvoiceItem
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    TaxRate = 0.15m,   // ضريبة القيمة المضافة السعودية
                    Total = lineTotal,
                });

                invoice.SubTotal += lineTotal;
                invoice.TaxAmount += Math.Round(lineTotal * 0.15m, 2);
            }

            invoice.Total = invoice.SubTotal - invoice.Discount + invoice.TaxAmount;
            return invoice;
        }

        /// <summary>مواصفة فاتورة داخل بيانات البذور.</summary>
        private sealed record SeedInvoice(
            int CustomerIndex,
            int DaysAgo,
            InvoiceStatus Status,
            int PaidAfterDays,
            int CreatedByIndex,
            (int ProductIndex, decimal Quantity)[] Items);
    }
}
