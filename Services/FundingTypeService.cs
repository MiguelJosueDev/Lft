using LiveFree.Core.Service;
using Lft.Generated.Interfaces;
using Lft.Generated.Models;
using Lft.Generated.Repositories.SqlServer;
using Microsoft.Extensions.Logging;

namespace Lft.Generated.Services;

public class FundingTypesService(
    IFundingTypesRepository repository,
    ILogger<FundingTypeModel> logger)
    : BaseModelService<IFundingTypesRepository, FundingTypeModel, long>(repository, logger), IFundingTypesService
{
}
