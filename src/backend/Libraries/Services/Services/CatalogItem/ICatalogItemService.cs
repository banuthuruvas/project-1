namespace Domain.Services.CatalogItem;

public interface ICatalogItemService : IBaseService<Models.CatalogItem>
{
    Task<IList<Models.CatalogItem>> GetByVendorAsync(int vendorId);
}
