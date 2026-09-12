using System.Windows;

namespace MizanPro.Windows
{
    /// <summary>
    /// شاشة البداية — تُعرض لمدة 3 ثوانٍ أثناء تهيئة قاعدة البيانات
    /// (إغلاقها يُدار من App.xaml.cs وليس من هنا).
    /// </summary>
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }
    }
}
