using System.Windows.Media;

namespace MizanPro.Controls
{
    /// <summary>
    /// أدوات بناء الأفاتار (الدائرة الملونة بأول حرفين من الاسم).
    /// </summary>
    public static class AvatarHelper
    {
        /// <summary>لوحة ألوان ثابتة تُوزَّع على العملاء بشكل حتمي (نفس الاسم = نفس اللون).</summary>
        private static readonly Color[] Palette =
        {
            Color.FromRgb(0x25, 0x63, 0xEB),   // أزرق
            Color.FromRgb(0x7C, 0x3A, 0xED),   // بنفسجي
            Color.FromRgb(0xDB, 0x27, 0x77),   // وردي
            Color.FromRgb(0xDC, 0x26, 0x26),   // أحمر
            Color.FromRgb(0xEA, 0x58, 0x0C),   // برتقالي
            Color.FromRgb(0xCA, 0x8A, 0x04),   // أصفر داكن
            Color.FromRgb(0x16, 0xA3, 0x4A),   // أخضر
            Color.FromRgb(0x08, 0x91, 0xB2),   // سماوي
        };

        /// <summary>لون حتمي مشتق من الاسم.</summary>
        public static SolidColorBrush BrushFor(string name)
        {
            int hash = 0;
            foreach (char c in name ?? string.Empty)
                hash = (hash * 31 + c) % 100003;

            var brush = new SolidColorBrush(Palette[Math.Abs(hash) % Palette.Length]);
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// أول حرفين من الاسم الصحيح (تُتخطى الكلمات العامة: شركة/مؤسسة/مجموعة/مكتب).
        /// مثال: "شركة الوطنية للتجارة" → "وال".
        /// </summary>
        public static string Initials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "؟";

            string[] generic = { "شركة", "مؤسسة", "مجموعة", "مكتب" };
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Where(w => !generic.Contains(w))
                            .ToList();

            if (words.Count == 0)
                words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (words.Count == 0)
                return name.Trim()[..1];

            // أول حرف من أول كلمتين (أو أول حرفين من الكلمة الوحيدة)
            if (words.Count == 1)
                return words[0].Length >= 2 ? words[0][..2] : words[0];

            return $"{words[0][0]}{words[1][0]}";
        }
    }
}
