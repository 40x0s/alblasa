namespace MizanPro.Models
{
    /// <summary>أدوار المستخدمين في النظام.</summary>
    public enum UserRole
    {
        /// <summary>مدير النظام — صلاحيات كاملة.</summary>
        Admin = 0,

        /// <summary>محاسب — إدارة الفواتير والعملاء والمنتجات.</summary>
        Accountant = 1,

        /// <summary>مشاهد — اطلاع فقط بدون تعديل.</summary>
        Viewer = 2,
    }

    /// <summary>مستخدم في النظام.</summary>
    public sealed class User
    {
        public int Id { get; set; }

        /// <summary>اسم المستخدم للدخول (فريد على مستوى النظام).</summary>
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        /// <summary>كلمة المرور مخزّنة كـ BCrypt Hash — لا تُخزّن أبداً كنص صريح.</summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>الاسم الكامل للعرض في الواجهة.</summary>
        public string FullName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Viewer;

        /// <summary>تاريخ آخر تسجيل دخول (null إذا لم يدخل بعد).</summary>
        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
    }
}
