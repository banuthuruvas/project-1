using Data.Data;

namespace Domain.Services.Document;

public class DocumentService : BaseService<Domain.Models.Document>, IDocumentService
{
    public DocumentService(MainDbContext context)
        : base(context)
    { }
}
