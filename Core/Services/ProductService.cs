using Microsoft.EntityFrameworkCore;
using MizanPro.Data;
using MizanPro.Models;

namespace MizanPro.Core.Services
{
    /// <summary>
    /// خدمة المنتجات (Singleton): البحث، الإنشاء، تعديل المخزون، والأصناف منخفضة المخزون.
    /// </summary>
    public sealed class ProductService
    {
        /// <summary>المثال الوحيد على مستوى التطبيق.</summary>
        public static ProductService Instance { get; } = new ProductService();

        private ProductService()
        {
        }

        /// <summary>كل المنتجات مع بحث اختياري (بالاسم أو SKU) وتصفية اختيارية بالتصنيف.</summary>
        public async Task<List<Product>> GetAllAsync(string? search = null, string? category = null)
        {
            using var db = new MizanDbContext();

            var query = db.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category == category);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(p => p.Name.Contains(term) || p.SKU.Contains(term));
            }

            return await query
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        /// <summary>إنشاء منتج جديد مع التحقق من فريدة الـ SKU.</summary>
        public async Task<Product> CreateAsync(
            string name,
            string sku,
            decimal price,
            decimal cost,
            int stock,
            string unit,
            string category)
        {
            string trimmedName = (name ?? string.Empty).Trim();
            string trimmedSku = (sku ?? string.Empty).Trim();

            if (trimmedName.Length == 0)
                throw new InvalidOperationException("اسم المنتج مطلوب.");

            if (trimmedSku.Length == 0)
                throw new InvalidOperationException("الرقم التعريفي SKU مطلوب.");

            if (price < 0)
                throw new InvalidOperationException("سعر البيع لا يمكن أن يكون سالباً.");

            if (cost < 0)
                throw new InvalidOperationException("سعر التكلفة لا يمكن أن يكون سالباً.");

            if (stock < 0)
                throw new InvalidOperationException("المخزون لا يمكن أن يكون سالباً.");

            using var db = new MizanDbContext();

            bool skuExists = await db.Products.AnyAsync(p => p.SKU == trimmedSku);
            if (skuExists)
                throw new InvalidOperationException($"يوجد منتج آخر بنفس الرقم التعريفي ({trimmedSku}).");

            var product = new Product
            {
                Name = trimmedName,
                SKU = trimmedSku,
                Price = price,
                CostPrice = cost,
                Stock = stock,
                Unit = string.IsNullOrWhiteSpace(unit) ? "قطعة" : unit.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? "عام" : category.Trim(),
                IsActive = true,
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product;
        }

        /// <summary>
        /// تعديل المخزون بفارق موجب (توريد) أو سالب (صرف).
        /// يعيد false إذا لم يوجد المنتج، ويرفض أن يصبح المخزون سالباً.
        /// </summary>
        public async Task<bool> AdjustStockAsync(int id, int adjustment)
        {
            using var db = new MizanDbContext();

            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
                return false;

            int newStock = product.Stock + adjustment;
            if (newStock < 0)
                throw new InvalidOperationException("لا يمكن أن يصبح المخزون سالباً.");

            product.Stock = newStock;
            await db.SaveChangesAsync();

            return true;
        }

        /// <summary>المنتجات النشطة التي وصل مخزونها إلى الحد الأدنى (5 افتراضياً).</summary>
        public async Task<List<Product>> GetLowStockAsync(int threshold = 5)
        {
            using var db = new MizanDbContext();

            return await db.Products
                .Where(p => p.IsActive && p.Stock <= threshold)
                .OrderBy(p => p.Stock)
                .ToListAsync();
        }
    }
}
