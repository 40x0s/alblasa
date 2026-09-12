namespace MizanPro.Core.Services
{
    /// <summary>
    /// مُدخل صنف واحد عند إنشاء فاتورة جديدة.
    /// (يحدد المنتج والكمية والسعر المتفق عليه فقط — أما الضريبة والمجاميع فيحسبها InvoiceService.)
    /// </summary>
    public sealed record InvoiceItemInput(int ProductId, decimal Quantity, decimal UnitPrice);
}
