using ContractApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContractsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ContractsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("by-customer/{customerId}")]
    public async Task<IActionResult> GetContracts(int customerId)
    {
        var contracts = await _context.Contracts
            .Include(c => c.AmortPlans)
            .Where(c => c.CustomerId == customerId)
            .Select(c => new
            {
                c.ContractNumber,
                Plans = c.AmortPlans.Select(a => new
                {
                    a.ClaimDueDate,
                    a.TotalAmount,
                    a.PaidAmount,
                    a.DueAmount,
                    a.CurrencyCode
                }).ToList(),
                Summary = new
                {
                    TotalPaid = c.AmortPlans.Sum(a => a.PaidAmount),
                    TotalDue = c.AmortPlans.Sum(a => a.DueAmount),
                    PastDue = c.AmortPlans
                        .Where(a => a.ClaimDueDate <= DateTime.Today)
                        .Sum(a => a.DueAmount)
                }
            })
            .ToListAsync();

        if (!contracts.Any())
            return NotFound("No contracts found.");

        var totalPaidAllContracts = contracts.Sum(c => c.Summary.TotalPaid);

        var result = new
        {
            totalPaidAllContracts,
            contracts
        };

        return Ok(result);
    }


    [HttpGet("summary-by-customer/{customerId}")]
    public async Task<IActionResult> GetSummary(int customerId)
    {
        var contracts = await _context.Contracts
            .Include(c => c.AmortPlans)
            .Where(c => c.CustomerId == customerId)
            .ToListAsync();

        var summary = new
        {
            TotalPaid = contracts.SelectMany(c => c.AmortPlans).Sum(p => p.PaidAmount),
            TotalDue = contracts.SelectMany(c => c.AmortPlans).Sum(p => p.DueAmount),
            PastDue = contracts.SelectMany(c => c.AmortPlans)
                        .Where(p => p.ClaimDueDate <= DateTime.Today)
                        .Sum(p => p.DueAmount)
        };

        return Ok(summary);
    }

    

}
