namespace MizanPro.Controls
{
    /// <summary>
    /// أي صفحة تُطبّق هذه الواجهة يستدعيها MainWindow عند كل تنقّل إليها
    /// لإعادة تحميل بياناتها.
    /// </summary>
    public interface IRefreshable
    {
        /// <summary>إعادة تحميل بيانات الصفحة (غير حاجب — يعمل في الخلفية).</summary>
        void Refresh();
    }
}
