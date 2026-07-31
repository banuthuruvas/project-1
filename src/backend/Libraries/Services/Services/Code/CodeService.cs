using Microsoft.EntityFrameworkCore;
using Data.Data;
using Domain.Enum;

namespace Domain.Services.Code;

public class CodeService : BaseService<Domain.Models.Code>, ICodeService
{
    public CodeService(MainDbContext context) : base(context)
    { }

    public async Task<IList<Domain.Models.Code>> GetAllByCodeType(ECodeType codeType)
    {
        return await Records.Where(x => x.Type == codeType.ToString()).ToListAsync();
    }
}
