// ============================================================================
// فحص تشغيلي (Smoke Test) لميزان برو — يُنفَّذ داخل GitHub Actions فقط.
// يتحقق فعلياً من: قاعدة البيانات + الـ Migrations + البيانات التجريبية +
// تسجيل الدخول + إنشاء الفواتير (الضريبة والمخزون) + تحديث الحالة +
// التقارير والإحصاءات + تغيير كلمة المرور + محرك الحماية الخفي
// (TokenProcessor + ProductStateEngine: Registry + state.enc + AES + بصمة الجهاز).
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using MizanPro.Core;
using MizanPro.Core.Engine;
using MizanPro.Core.Services;
using MizanPro.Data;
using MizanPro.Models;

namespace MizanPro.SmokeTest
{
    internal static class Program
    {
        private static int _failures;

        // مفتاحا تفعيل صحيحان (محسوبان ومُتحقَّق منهما — انظر تعليق TokenProcessor.ParseAndVerify)
        private const string ValidKeyDashed = "M1Z4N-2025P-ROM4S-T3R01";   // مقروء
        private const string ValidKeyPlain = "AAAAABBBBBCCCCCADN89";       // من عائلة مثال المواصفة

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

            // ─── 8) محرك الحماية الخفي: TokenProcessor + ProductStateEngine ───
            await RunProtectionEngineChecksAsync();

            Console.WriteLine(_failures == 0 ? ">>> ALL CHECKS PASSED <<<" : ">>> " + _failures + " CHECK(S) FAILED <<<");
            return _failures == 0 ? 0 : 1;
        }

        // ═══════════ فحص محرك الحماية (Registry + state.enc + AES + المفاتيح) ═══════════

        private static async Task RunProtectionEngineChecksAsync()
        {
            Console.WriteLine("─── محرك الحماية ───");

            // تنظيف أي حالة سابقة: شجرة الـ Registry كاملة + ملف state.enc
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\MizanSolutions", throwOnMissingSubKey: false);
            File.Delete(MizanPaths.StateFilePath);

            // 8-أ) خوارزمية التحقق من المفاتيح
            Check(TokenProcessor.ParseAndVerify(ValidKeyDashed), "قبول المفتاح الصحيح (بالشرطات)");
            Check(TokenProcessor.ParseAndVerify(ValidKeyPlain), "قبول المفتاح الصحيح (بدون شرطات)");
            Check(TokenProcessor.ParseAndVerify(ValidKeyDashed.Replace("-", "").ToLowerInvariant()),
                "قبول المفتاح بأحرف صغيرة (ToUpper قبل الفحص)");
            Check(!TokenProcessor.ParseAndVerify("AAAAABBBBBCCCCCDE77A"),
                "رفض مثال المواصفة الأصلي (لا يحقق شرطي المجموع وXOR)");
            Check(!TokenProcessor.ParseAndVerify("M1Z4N-2025P-ROM4S-T3R0"),     // 19 حرفاً
                "رفض مفتاح ناقص حرف");
            Check(!TokenProcessor.ParseAndVerify("M1Z4N-2025P-ROM4S-T3R012"),    // 21 حرفاً
                "رفض مفتاح زائد حرف");
            Check(!TokenProcessor.ParseAndVerify("M1Z4N-2025P-ROM4S-T3R0!"),     // محرف غير مسموح
                "رفض محرف خارج الأبجدية");
            Check(!TokenProcessor.ParseAndVerify(""), "رفض مفتاح فارغ");

            // 8-ب) بصمة الجهاز بصيغة XXXX-XXXX-XXXX-XXXX (16 خانة hex كبيرة)
            string fingerprint = TokenProcessor.ComputeMachineFingerprint();
            Check(System.Text.RegularExpressions.Regex.IsMatch(fingerprint, @"^[0-9A-F]{4}(-[0-9A-F]{4}){3}$"),
                "صيغة بصمة الجهاز صحيحة: " + fingerprint);

            // 8-ج) أول تشغيل نظيف: تجربة 7 أيام سارية
            bool firstRun = ProductStateEngine.Instance.EvaluateCurrentState();
            Check(firstRun, "أول تشغيل: السماح بالعمل (تجربة)");
            Check(ProductStateEngine.Plan == "TRIAL", "الخطة = TRIAL");
            Check(!ProductStateEngine.IsVerified, "غير مفعّل بعد");
            Check(ProductStateEngine.TrialDaysLeft == ProductStateEngine.TrialPeriodDays,
                "أيام التجربة المتبقية = " + ProductStateEngine.TrialPeriodDays);

            // 8-د) محاكاة تجربة منتهية (تاريخ تركيب قديم)
            SetRegistryValue("InstallDate", DateTime.Today.AddDays(-10).ToString("yyyyMMdd"));
            bool expired = ProductStateEngine.Instance.EvaluateCurrentState();
            Check(!expired, "تجربة منتهية → رفض التشغيل");
            Check(ProductStateEngine.TrialDaysLeft <= 0, "الأيام المتبقية سالبة");

            // 8-هـ) التفعيل بمفتاح صحيح
            SetRegistryValue("InstallDate", DateTime.Today.ToString("yyyyMMdd"));
            var (activationOk, activationMsg) = await ProductStateEngine.Instance.CommitActivation(ValidKeyDashed);
            Check(activationOk && activationMsg == "تم تفعيل ميزان Pro بنجاح!",
                "CommitActivation بمفتاح صحيح: " + activationMsg);

            // 8-و) الحالة بعد التفعيل
            bool afterActivation = ProductStateEngine.Instance.EvaluateCurrentState();
            Check(afterActivation && ProductStateEngine.IsVerified && ProductStateEngine.Plan == "PRO",
                "بعد التفعيل: PRO ومسموح بالتشغيل");

            string? storedBlob = GetRegistryValue("ActivationBlob");
            Check(storedBlob == ValidKeyDashed, "ActivationBlob مخزّن في الـ Registry كما أُدخل");
            Check(File.Exists(MizanPaths.StateFilePath), "ملف state.enc موجود");

            // 8-ز) العبث بالـ Registry: القيمة المعدَّلة تُحذف
            //      والمسار الاحتياطي (state.enc) يبقي البرنامج شغّالاً
            SetRegistryValue("ActivationBlob", "AAAAABBBBBCCCCCDE77A");   // قيمة غير صالحة
            bool tampered = ProductStateEngine.Instance.EvaluateCurrentState();
            bool blobDeleted = GetRegistryValue("ActivationBlob") is null;
            Check(tampered && blobDeleted,
                "حذف Blob المُعبَّث به + الاستمرار في العمل عبر المسار الاحتياطي state.enc");

            // 8-ح) ملف state.enc مرتبط بالجهاز: بصمة خاطئة لا تُعتمد
            //      (نكتب ملفاً مشفّراً بمحتوى بصمة مختلفة ثم نثبت أنه لا يُنقذ تجربةً منتهية)
            File.WriteAllText(MizanPaths.StateFilePath, EncryptForTest("MIZAN_VALID|AAAA-BBBB-CCCC-DDDD"));
            SetRegistryValue("InstallDate", DateTime.Today.AddDays(-10).ToString("yyyyMMdd"));
            bool wrongFingerprint = ProductStateEngine.Instance.EvaluateCurrentState();
            Check(!wrongFingerprint, "بصمة خاطئة في state.enc + تجربة منتهية → رفض (ربط الملف بالجهاز يعمل)");

            // 8-ط) تنظيف نهائي: إعادة التفعيل والانتهاء بحالة PRO
            File.Delete(MizanPaths.StateFilePath);
            var (reactivated, reactivatedMsg) = await ProductStateEngine.Instance.CommitActivation(ValidKeyPlain);
            Check(reactivated && ProductStateEngine.Instance.EvaluateCurrentState(),
                "إعادة تفعيل نهائية بمفتاح آخر: " + reactivatedMsg);
        }

        // ─────────────── مساعدات الـ Registry والـ AES للاختبار ───────────────

        private static void SetRegistryValue(string name, string value)
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(ProductStateEngine.RegistryPath, writable: true);
            key.SetValue(name, value, RegistryValueKind.String);
        }

        private static string? GetRegistryValue(string name)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ProductStateEngine.RegistryPath);
            return key?.GetValue(name) as string;
        }

        /// <summary>تشفير بنفس مفتاح المحرك — لمحاكاة ملف state.enc بصبغة خاطئة.</summary>
        private static string EncryptForTest(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes("Mizan2024_K3y128");
            aes.IV = Encoding.UTF8.GetBytes("MizanPro_IV_2024");
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(encryptor.TransformFinalBlock(bytes, 0, bytes.Length));
        }

        // ─────────────── أداة الفحص ───────────────

        private static void Check(bool condition, string name)
        {
            Console.WriteLine((condition ? "  [OK]   " : "  [FAIL] ") + name);
            if (!condition)
                _failures++;
        }
    }
}
