using Domain.Enum;

namespace Domain.Services.Code;

public interface ICodeService : IBaseService<Domain.Models.Code>
{
    Task<IList<Domain.Models.Code>> GetAllByCodeType(ECodeType codeType);
}
