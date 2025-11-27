using Microsoft.Extensions.DependencyInjection;
using RepoDb;
using LiveFree.Core.Validation;
using LiveFree.Core.Repository.Extensions;
using LiveFree.Accounts.Models;
using LiveFree.Accounts.Repositories.SqlServer.Entities;
using LiveFree.Accounts.Repositories.SqlServer.Mappers;

namespace LiveFree.Accounts.Repositories.SqlServer.Extensions;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddAccountsRepositories(
        this IServiceCollection services)
    {
        services.RequireThat().NotNull();

        services.AddAutoMapper(typeof(AccountsMappingProfile));

        GlobalConfiguration.Setup().UseSqlServer();

        services.AddScoped<IAccountsRepository, AccountsRepository>();
        services.AddScoped<IAccountNotesRepository, AccountNotesRepository>();
        services.AddScoped<IDealersRepository, DealersRepository>();
        services.AddScoped<IAddressesRepository, AddressesRepository>();
        services.AddScoped<IPhoneTypesRepository, PhoneTypesRepository>();

        services.AddMqlQueries<IAccountsConnectionFactory, IAccountsUnitOfWork>(options =>
        {
            options.AddMqlQuery<DealerModel, DealerEntity>();
            options.AddMqlQuery<PhoneTypeModel, PhoneTypeEntity>();
        });

        return services;
    }
}
