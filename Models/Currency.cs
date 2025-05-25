using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContractApi.Models;

[Table("currencies")]
public class Currency
{
    [Key]
    [Column("currency_code")]
    public string CurrencyCode { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("inactive")]
    public bool Inactive { get; set; }
}
