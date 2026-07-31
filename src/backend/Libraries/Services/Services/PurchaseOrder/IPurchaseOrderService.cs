using Domain.Dto;
using Domain.Enum;

namespace Domain.Services.PurchaseOrder;

public interface IPurchaseOrderService : IBaseService<Models.PurchaseOrder>
{
    Task<Models.PurchaseOrder?> GetByIdWithDetailsAsync(int id);
    Task<IList<Models.PurchaseOrder>> GetAllWithVendorAsync();
    Task<string> GeneratePoNumberAsync();
    Task<(IList<Models.PurchaseOrder> Items, int TotalCount)> SearchAsync(PurchaseOrderSearchDto filter);
    Task<SpendOverviewDto> GetSpendOverviewAsync();
    Task<IList<Models.PurchaseOrder>> GetPendingApprovalsAsync(string userId);
}
