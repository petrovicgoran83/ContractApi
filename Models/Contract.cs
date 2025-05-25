using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContractApi.Models;

[Table("contracts")]
public class Contract
{
    [Key]
    [Column("contract_id")]
    public int ContractId { get; set; }

    [Column("contract_number")]
    public string ContractNumber { get; set; } = string.Empty;

    [Column("customer_id")]
    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public ICollection<AmortPlan> AmortPlans { get; set; } = new List<AmortPlan>();
}
