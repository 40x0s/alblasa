using Microsoft.EntityFrameworkCore;
using MizanPro.Core;
using MizanPro.Models;

namespace MizanPro.Data
{
    /// <summary>
    /// سياق قاعدة البيانات الأساسي (SQLite).
    /// الملف الفعلي: %AppData%\MizanPro\mizan.db
    /// </summary>
    public sealed class MizanDbContext : DbContext
    {
        /// <summary>الجداول — المستخدمون.</summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>الجداول — العملاء.</summary>
        public DbSet<Customer> Customers => Set<Customer>();

        /// <summary>الجداول — المنتجات.</summary>
        public DbSet<Product> Products => Set<Product>();

        /// <summary>الجداول — الفواتير.</summary>
        public DbSet<Invoice> Invoices => Set<Invoice>();

        /// <summary>الجداول — أصناف الفواتير.</summary>
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // قاعدة بيانات SQLite في مجلد بيانات المستخدم
            optionsBuilder.UseSqlite($"Data Source={MizanPaths.DatabaseFilePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ─────────────── المستخدمون ───────────────
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();       // فهرس فريد: اسم المستخدم
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired();
            });

            // ─────────────── العملاء ───────────────
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
                entity.Property(c => c.Phone).IsRequired().HasMaxLength(20);
                entity.Property(c => c.City).IsRequired().HasMaxLength(60);
                entity.Property(c => c.TaxNumber).HasMaxLength(15);
                entity.HasIndex(c => c.Name);                      // فهرس عادي لتسريع البحث
            });

            // ─────────────── المنتجات ───────────────
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(p => p.SKU).IsUnique();            // فهرس فريد: SKU
                entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
                entity.Property(p => p.SKU).IsRequired().HasMaxLength(30);
                entity.Property(p => p.Unit).IsRequired().HasMaxLength(20);
                entity.Property(p => p.Category).IsRequired().HasMaxLength(60);
            });

            // ─────────────── الفواتير ───────────────
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(i => i.InvoiceNumber).IsUnique();  // فهرس فريد: رقم الفاتورة
                entity.HasIndex(i => i.IssueDate);                 // لتسريع البحث بالتاريخ
                entity.HasIndex(i => i.Status);

                entity.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(20);
                entity.Property(i => i.Notes).HasMaxLength(500);

                // كل فاتورة تخص عميلاً واحداً — يُمنع حذف عميل لديه فواتير
                entity.HasOne(i => i.Customer)
                      .WithMany(c => c.Invoices)
                      .HasForeignKey(i => i.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                // كل فاتورة أنشأها مستخدم واحد — يُمنع حذف مستخدم لديه فواتير
                entity.HasOne(i => i.CreatedBy)
                      .WithMany()
                      .HasForeignKey(i => i.CreatedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ─────────────── أصناف الفواتير ───────────────
            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                // حذف الفاتورة يحذف أصنافها تلقائياً
                entity.HasOne(item => item.Invoice)
                      .WithMany(i => i.Items)
                      .HasForeignKey(item => item.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);

                // لا يمكن حذف منتج مستخدم في فواتير
                entity.HasOne(item => item.Product)
                      .WithMany(p => p.InvoiceItems)
                      .HasForeignKey(item => item.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
