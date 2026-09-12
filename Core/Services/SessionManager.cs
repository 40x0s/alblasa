using Microsoft.EntityFrameworkCore;
using MizanPro.Data;
using MizanPro.Models;

namespace MizanPro.Core.Services
{
    /// <summary>
    /// مدير الجلسة (Singleton): يحتفظ بالمستخدم الحالي ويدير تسجيل الدخول والخروج
    /// وتغيير كلمة المرور.
    /// </summary>
    public sealed class SessionManager
    {
        /// <summary>المثال الوحيد على مستوى التطبيق.</summary>
        public static SessionManager Instance { get; } = new SessionManager();

        private SessionManager()
        {
        }

        /// <summary>المستخدم الذي سجّل الدخول (null إذا لم يسجّل أحد).</summary>
        public User? ActiveUser { get; private set; }

        /// <summary>هل يوجد مستخدم مسجّل للدخول حالياً؟</summary>
        public bool IsAuthenticated => ActiveUser is not null;

        /// <summary>هل المستخدم الحالي مدير نظام؟</summary>
        public bool IsAdmin => ActiveUser?.Role == UserRole.Admin;

        /// <summary>
        /// تسجيل الدخول: التحقق من المدخلات ثم التحقق من كلمة المرور عبر BCrypt.
        /// </summary>
        /// <returns>ثلاثية: (نجح؟ ، رسالة خطأ عربية أو null ، كائن المستخدم أو null).</returns>
        public async Task<(bool Ok, string? Error, User? User)> SignInAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "الرجاء إدخال اسم المستخدم.", null);

            if (string.IsNullOrWhiteSpace(password))
                return (false, "الرجاء إدخال كلمة المرور.", null);

            using var db = new MizanDbContext();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username.Trim());

            // رسالة واحدة موحّدة لعدم وجود المستخدم أو خطأ كلمة المرور (حماية من التخمين)
            if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return (false, "اسم المستخدم أو كلمة المرور غير صحيحة.", null);

            if (!user.IsActive)
                return (false, "هذا الحساب موقوف — الرجاء مراجعة مدير النظام.", null);

            // تحديث تاريخ آخر دخول
            user.LastLoginAt = DateTime.Now;
            await db.SaveChangesAsync();

            ActiveUser = user;
            return (true, null, user);
        }

        /// <summary>تسجيل الخروج ومسح الجلسة الحالية.</summary>
        public void SignOut() => ActiveUser = null;

        /// <summary>
        /// تغيير كلمة مرور المستخدم الحالي بعد التحقق من كلمة المرور القديمة.
        /// يعيد false إذا: لم يكن مسجّلاً للدخول، أو كلمة المرور القديمة خاطئة،
        /// أو الكلمة الجديدة أقصر من 6 أحرف.
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            if (ActiveUser is null)
                return false;

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return false;

            using var db = new MizanDbContext();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == ActiveUser.Id);
            if (user is null || !BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await db.SaveChangesAsync();

            return true;
        }
    }
}
