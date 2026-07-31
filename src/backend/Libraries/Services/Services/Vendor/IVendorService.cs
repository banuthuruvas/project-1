using Domain.Models;

namespace Domain.Services.Vendor;

public interface IVendorService : IBaseService<Models.Vendor>
{
    Task<IList<Models.Vendor>> GetAllWithCatalogCountAsync();
}
