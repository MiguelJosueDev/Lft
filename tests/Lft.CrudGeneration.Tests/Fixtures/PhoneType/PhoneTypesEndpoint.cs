using LiveFree.Core.AspNetCore.Endpoints;
using LiveFree.Core.Service.Authorization.Interfaces;
using LiveFree.Accounts.Interfaces;
using LiveFree.Accounts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace LiveFree.Accounts.Api.Endpoints;

public interface IPhoneTypesEndpoint : IModelEndpoint<PhoneTypeModel, byte>
{
    Task<IResult> QueryAsync(string query);
}

public class PhoneTypesEndpoint(
    IPhoneTypesService service,
    IAuthorizationService authorizationService,
    IAuthorizationContextService authorizationContextService,
    IHttpContextAccessor httpContextAccessor)
    : ModelEndpoint<PhoneTypeModel, IPhoneTypesService, byte>(service,
        authorizationService, authorizationContextService, httpContextAccessor), IPhoneTypesEndpoint
{
    public async Task<IResult> QueryAsync(string query)
        => string.IsNullOrWhiteSpace(query)
            ? Results.BadRequest("Query cannot be empty.")
            : TypedResults.Ok(await Service.QueryMqlAsync(query));

}
