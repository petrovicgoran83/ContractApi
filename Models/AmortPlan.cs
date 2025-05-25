using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContractApi.Models;

[Table("amort_plan")]
public class AmortPlan
{
    [Key]
    [Column("document_id")]
    public string DocumentId { get; set; } = string.Empty;

    [Column("contract_id")]
    public int ContractId { get; set; }

    public Contract? Contract { get; set; }

    [Column("claim_due_date")]
    public DateTime ClaimDueDate { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("paid_amount")]
    public decimal PaidAmount { get; set; }

    [Column("due_amount")]
    public decimal DueAmount { get; set; }

    [Column("currency_code")]
    public string CurrencyCode { get; set; } = string.Empty;
}
