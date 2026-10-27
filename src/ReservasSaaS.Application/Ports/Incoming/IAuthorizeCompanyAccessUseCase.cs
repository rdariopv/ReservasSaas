using ReservasSaaS.Domain.Entities;
using ReservasSaaS.Domain.Enums;

namespace ReservasSaaS.Application.Ports.Incoming;

public record CompanyAuthorizationResult(
    bool IsAuthorized,
    Company Company,
    CompanyRole Role,
    IReadOnlySet<string> EffectivePermissions);

public interface IAuthorizeCompanyAccessUseCase
{
    Task<CompanyAuthorizationResult> AuthorizeAsync(
        Guid companyId, 
        string? requiredPermission = null, 
        CancellationToken cancellationToken = default);
}
