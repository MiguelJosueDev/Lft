using LiveFree.Core.AspNetCore.Endpoints;
using LiveFree.Core.Service.Authorization.Interfaces;
using Lft.Generated.Interfaces;
using Lft.Generated.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Lft.Generated.Api.Endpoints;

public interface IFundingTypesEndpoint : IModelEndpoint<FundingTypeModel, long>
{
}

public class FundingTypesEndpoint(
    IFundingTypesService service,
    IAuthorizationService authorizationService,
    IAuthorizationContextService authorizationContextService,
    IHttpContextAccessor httpContextAccessor)
    : ModelEndpoint<FundingTypeModel, IFundingTypesService, long>(service,
        authorizationService, authorizationContextService, httpContextAccessor), IFundingTypesEndpoint
{

}
