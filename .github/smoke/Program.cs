// ============================================================================
// فحص تشغيلي (Smoke Test) لميزان برو — يُنفَّذ داخل GitHub Actions فقط.
// يتحقق فعلياً من: قاعدة البيانات + الـ Migrations + البيانات التجريبية +
// تسجيل الدخول + إنشاء الفواتير (الضريبة والمخزون) + تحديث الحالة +
// التقارير والإحصاءات + تغيير كلمة المرور + محرك التجربة والتفعيل.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MizanPro.Core;
using MizanPro.Core.Licensing;
using MizanPro.Core.Services;
using MizanPro.Data;
using MizanPro.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MizanPro.SmokeTest
{
    internal static class Program
    {
        private static int _failures;

        private static async Task<int> Main()
        {
            Console.WriteLine("=== MizanPro Smoke Test ===");

            // ─── 1) قاعدة البيانات + الـ Migrations + البيانات التجريبية ───
            MizanPaths.EnsureAppDataFolder();
            using (var db = new MizanDbContext())
            {
                await db.Database.MigrateAsync();
                if (!await db.Users.AnyAsync())
                    await DataSeeder.SeedAsync(db);

                Check(await db.Users.CountAsync() == 2, "عدد المستخدمين = 2");
                Check(await db.Customers.CountAsync() == 15, "عدد العملاء = 15");
                Check(await db.Products.CountAsync() == 20, "عدد المنتجات = 20");
                Check(await db.Invoices.CountAsync() == 12, "عدد الفواتير = 12");
                Check(await db.InvoiceItems.CountAsync() > 0, "توجد أصناف فواتير");
            }

            // ─── 2) تسجيل الدخول ───
            var (ok, error, user) = await SessionManager.Instance.SignInAsync("admin", "Admin@123");
            Check(ok, "تسجيل دخول admin: " + error);
            Check(user != null && user.Role == UserRole.Admin, "دور admin صحيح");

            var (badOk, _, _) = await SessionManager.Instance.SignInAsync("admin", "WrongPass");
            Check(!badOk, "رفض كلمة مرور خاطئة");

            // ─── 3) إنشاء فاتورة: ضريبة 15% + خصم مخزون + رقم تلقائي ───
            Product productBefore = (await ProductService.Instance.GetAllAsync()).First(p => p.Id == 1);
            Invoice invoice = await InvoiceService.Instance.CreateAsync(
                customerId: 1,
                items: new[] { new InvoiceItemInput(1, 2m, 100m) },
                notes: "فاتورة اختبار");

            Check(invoice.SubTotal == 200m, "المجموع قبل الضريبة = 200 (فعلي: " + invoice.SubTotal + ")");
            Check(invoice.TaxAmount == 30m, "الضريبة 15% = 30 (فعلي: " + invoice.TaxAmount + ")");
            Check(invoice.Total == 230m, "الإجمالي = 230 (فعلي: " + invoice.Total + ")");
            Check(invoice.InvoiceNumber.StartsWith("INV-"), "صيغة رقم الفاتورة: " + invoice.InvoiceNumber);
            Check(invoice.Status == InvoiceStatus.Issued, "حالة الفاتورة الجديدة = صادرة");

            Product productAfter = (await ProductService.Instance.GetAllAsync()).First(p => p.Id == 1);
            Check(productAfter.Stock == productBefore.Stock - 2,
                "نقص المخزون بعد البيع (" + productBefore.Stock + " ← " + productAfter.Stock + ")");

            // ─── 4) تحديث الحالة: الإلغاء يعيد المخزون ───
            bool cancelled = await InvoiceService.Instance.UpdateStatusAsync(invoice.Id, InvoiceStatus.Cancelled);
            Product productRestored = (await ProductService.Instance.GetAllAsync()).First(p => p.Id == 1);
            Check(cancelled && productRestored.Stock == productBefore.Stock, "إرجاع المخزون عند الإلغاء");

            // ─── 5) الفواتير المتأخرة + الملخص الشهري ───
            List<Invoice> overdue = await InvoiceService.Instance.GetOverdueAsync();
            Check(overdue.Count >= 2, "فواتير متأخرة >= 2 (فعلي: " + overdue.Count + ")");

            DateTime now = DateTime.Now;
            MonthlySummary summary = await InvoiceService.Instance.GetMonthlySummaryAsync(now.Year, now.Month);

            // نحسب المتوقع بشكل مستقل من القائمة الكاملة للتأكد من تطابق الملخص
            List<Invoice> all = await InvoiceService.Instance.GetAllAsync();
            int expectedCount = all.Count(i =>
                i.IssueDate.Year == now.Year &&
                i.IssueDate.Month == now.Month &&
                i.Status != InvoiceStatus.Cancelled);

            Check(summary.InvoiceCount == expectedCount,
                "عدد فواتير الشهر = " + expectedCount + " (فعلي: " + summary.InvoiceCount + ")");
            Check(summary.TotalSales == summary.PaidAmount + summary.OutstandingAmount,
                "المبيعات = المسدد + المتبقي");

            // ─── 6) إحصاءات العميل + البحث + المخزون المنخفض ───
            CustomerStats stats = await CustomerService.Instance.GetStatsAsync(1);
            Check(stats.TotalInvoices >= 1 && stats.TotalAmount > 0, "إحصاءات العميل رقم 1");

            List<Customer> search = await CustomerService.Instance.GetAllAsync("الوطنية");
            Check(search.Count == 1, "البحث عن 'الوطنية' = عميل واحد (فعلي: " + search.Count + ")");

            List<Product> lowStock = await ProductService.Instance.GetLowStockAsync();
            Check(lowStock.Count >= 3, "منتجات منخفضة المخزون >= 3 (فعلي: " + lowStock.Count + ")");

            // ─── 7) تغيير كلمة المرور ───
            bool changed = await SessionManager.Instance.ChangePasswordAsync("Admin@123", "NewPass@99");
            bool wrongOld = await SessionManager.Instance.ChangePasswordAsync("BadOld@1", "Whatever@1");
            bool restored = await SessionManager.Instance.ChangePasswordAsync("NewPass@99", "Admin@123");
            Check(changed && !wrongOld && restored, "تغيير كلمة المرور والتحقق منها");

            // ─── 8) محرك الترخيص: تجربة → رفض مفتاح خاطئ → تفعيل صحيح ───
            ProductStateResult state = await ProductStateEngine.EvaluateCurrentState();
            Check(state.State == ProductState.TrialActive || state.State == ProductState.Licensed,
                "الحالة الأولية = تجربة أو مفعّل (فعلي: " + state.State + ")");

            var (badKeyOk, badKeyError) = await ProductStateEngine.ActivateAsync("0000-0000-0000-0000");
            Check(!badKeyOk, "رفض مفتاح تفعيل خاطئ: " + badKeyError);

            // حساب المفتاح الصحيح بنفس خوارزمية المحرك (SHA256 لمعرّف الجهاز + الملح)
            string licenseJson = await File.ReadAllTextAsync(MizanPaths.LicenseFilePath);
            string machineId = JObject.Parse(licenseJson)["machineId"]?.ToString() ?? "";
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(machineId + "MIZAN-PRO-2024-SA"));
            string validKey = string.Join("-",
                Enumerable.Range(0, 4).Select(i => hash[i * 2].ToString("X2") + hash[i * 2 + 1].ToString("X2")));

            var (goodKeyOk, goodKeyError) = await ProductStateEngine.ActivateAsync(validKey);
            Check(goodKeyOk, "قبول المفتاح الصحيح: " + goodKeyError);

            ProductStateResult finalState = await ProductStateEngine.EvaluateCurrentState();
            Check(finalState.State == ProductState.Licensed,
                "الحالة بعد التفعيل = مفعّل (فعلي: " + finalState.State + ")");

            Console.WriteLine(_failures == 0 ? ">>> ALL CHECKS PASSED <<<" : ">>> " + _failures + " CHECK(S) FAILED <<<");
            return _failures == 0 ? 0 : 1;
        }

        private static void Check(bool condition, string name)
        {
            Console.WriteLine((condition ? "  [OK]   " : "  [FAIL] ") + name);
            if (!condition)
                _failures++;
        }
    }
}
