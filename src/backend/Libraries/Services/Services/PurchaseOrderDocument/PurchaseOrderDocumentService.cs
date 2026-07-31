using Data.Data;

namespace Domain.Services.PurchaseOrderDocument;

public class PurchaseOrderDocumentService : BaseService<Models.PurchaseOrderDocument>, IPurchaseOrderDocumentService
{
    public PurchaseOrderDocumentService(MainDbContext context) : base(context)
    { }
}
