using AutoMapper;
using LiveFree.Accounts.Models;
using LiveFree.Accounts.Repositories.SqlServer.Entities;

namespace LiveFree.Accounts.Repositories.SqlServer.Mappers;

public class AccountsMappingProfile : Profile
{
    public AccountsMappingProfile()
    {
        CreateMap<AccountModel, AccountEntity>().ReverseMap();
        CreateMap<AccountNoteModel, AccountNoteEntity>().ReverseMap();
        CreateMap<PhoneTypeModel, PhoneTypeEntity>().ReverseMap();

        CreateMap<DealerModel, DealerEntity>()
            .ForMember(E => E.Timezone, opt => opt.MapFrom(M => IanaToWindows(M.Timezone)))
            .ReverseMap()
            .ForMember(M => M.Timezone, opt => opt.MapFrom(E => WindowsToIana(E.Timezone)));

        CreateMap<AddressModel, AddressEntity>()
            .ReverseMap();

        CreateMap<AccountModel, DealerModel>()
            .ReverseMap()
            .ForMember(A => A.IsEntity, opt => opt.MapFrom(_ => true))
            .ForMember(A => A.Type, opt => opt.MapFrom(_ => Models.Enums.AccountType.Dealer));
    }

    #region Timezone Helpers
    private static string? IanaToWindows(string iana)
    {
        if (string.IsNullOrEmpty(iana)) return null;

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(iana, out var windows))
            return windows;

        if (TimeZoneInfo.FindSystemTimeZoneById(iana) != null)
            return iana;

        return "Mountain Standard Time";
    }

    private static string? WindowsToIana(string windows)
    {
        if (String.IsNullOrEmpty(windows)) return null;

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(windows, out var iana))
            return iana;

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(windows, out var result))
            return windows;

        return "America/Denver";
    }

    #endregion
}
