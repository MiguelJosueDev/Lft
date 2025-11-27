using LiveFree.Accounts.Api.Endpoints;
using LiveFree.Accounts.Api.Services;
using LiveFree.Accounts.Repositories.SqlServer.Extensions;
using LiveFree.Accounts.Services;
using LiveFree.Accounts.Services.Extensions;
using LiveFree.ServiceBus;
using LiveFree.Signaling.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LiveFree.Accounts.Api.Extensions;

public static class AccountsServicesExtensions
{
    public static WebApplicationBuilder AddAccountsServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddScoped<IAccountsAuthorizedApplicationService, AccountsAuthorizedApplicationService>();

        if (builder.Environment.EnvironmentName == "Development")
        {
            services.AddSingleton<ISignalingClient, MockSignalingClient>();
        }
        else
        {
            services.Configure<ServiceBusConfig>(builder.Configuration);
            services.AddSignalingClient();
        }

        services.AddAccountsRepositories();
        services.AddAccountsServices(builder.Environment.EnvironmentName);

        // Endpoints
        services.AddScoped<IAccountsEndpoint, AccountsEndpoint>();
        services.AddScoped<IMessagesEndpoint, MessagesEndpoint>();
        services.AddScoped<IDealersEndpoint, DealersEndpoint>();
        services.AddScoped<IPhoneTypesEndpoint, PhoneTypesEndpoint>();

        return builder;
    }
}
