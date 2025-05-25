using Microsoft.EntityFrameworkCore;
using ContractApi.Models;

namespace ContractApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<AmortPlan> AmortPlans => Set<AmortPlan>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<Currency> Currencies => Set<Currency>();
    


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExchangeRate>()
            .HasKey(e => new { e.CurrencyFrom, e.CurrencyTo, e.ExchangeRateDate });
        

    }
}
