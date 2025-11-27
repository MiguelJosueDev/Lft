using LiveFree.Accounts.Interfaces;
using LiveFree.Accounts.Services.ServiceBus;
using LiveFree.Core.Validation;
using LiveFree.ServiceBus;
using LiveFree.Signaling.Client;
using Microsoft.Extensions.DependencyInjection;

namespace LiveFree.Accounts.Services.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddAccountsServices(this IServiceCollection services, string environmentName = "Development")
    {
        services.RequireThat().NotNull();

        // Register the service bus
        services.AddServiceBus<IServiceBus, AccountsServiceBus>();

        // Register the service
        services.AddScoped<IAuditHistoryService, AuditHistoryService>();
        services.AddScoped<IAccountsService, AccountsService>();
        services.AddScoped<IAccountNotesService, AccountNotesService>();
        services.AddScoped<IMessagesService, MessagesService>();
        services.AddScoped<IDealersService, DealersService>();
        services.AddScoped<IPhoneTypesService, PhoneTypesService>();

        return services;
    }
}
