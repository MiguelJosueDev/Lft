using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using RepoDb.Attributes;

namespace Lft.Generated.Repositories.SqlServer.Entities;

// TODO: FundingType - Review Entity Configuration
[Table("", Schema = "")]
public class FundingTypeEntity
{
    [Key, Primary]
    public long Id { get; set; }
}
