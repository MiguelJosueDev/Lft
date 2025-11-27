using LiveFree.Accounts.Api.Routes;
using Microsoft.AspNetCore.Builder;

namespace LiveFree.Accounts.Api.Extensions;

public static class AccountsRoutesExtensions
{
    public static WebApplication AddAccountsRoutes(
        this WebApplication app,
        string basePrefix = "accounts")
    {
        app.MapAccountsRoutes(basePrefix: basePrefix);
        app.MapMessagesRoutes(basePrefix: basePrefix, prefix: "messages");
        app.MapDealersRoutes(basePrefix: basePrefix, prefix: "dealers");
        app.MapPhoneTypesRoutes(basePrefix: basePrefix, prefix: "phone-types");

        return app;
    }
}
