using LiveFree.Core.Service;
using LiveFree.Core.Service.MQL;
using LiveFree.Accounts.Models;

namespace LiveFree.Accounts.Interfaces;

public interface IPhoneTypesService : IModelService<PhoneTypeModel, byte>, IMqlService<PhoneTypeModel>
{

}
