using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using RepoDb.Attributes;

namespace LiveFree.Accounts.Repositories.SqlServer.Entities;

[Table("PhoneTypes", Schema = "dbo")]
public class PhoneTypeEntity
{
    [Key, Primary]
    [Column("PhoneTypeID"), TypeMap(DbType.Byte)]
    public byte Id { get; set; }
    public string? PhoneTypeName { get; set; }
    public string? PhoneTypeDesc { get; set; }
}
