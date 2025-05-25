using System.ComponentModel.DataAnnotations.Schema;

namespace ContractApi.Models;

[Table("exchange_rates")]
public class ExchangeRate
{
    [Column("currency_from")]
    public string CurrencyFrom { get; set; } = string.Empty;

    [Column("currency_to")]
    public string CurrencyTo { get; set; } = "RSD";

    [Column("exchange_rate_date")]
    public DateTime ExchangeRateDate { get; set; }

    [Column("exchange_rate")]
    public decimal Rate { get; set; }

    [Column("ts")]
    public DateTime Ts { get; set; } = DateTime.Now;
}
