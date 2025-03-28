using Microsoft.EntityFrameworkCore;
using LondonStockAPI.Model;

namespace LondonStockAPI.Repository
{
    public class StockExchangeContext : DbContext
    {
        public StockExchangeContext(DbContextOptions<StockExchangeContext> options) : base(options)
        {
        }

        public DbSet<Trade> Trades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Trade>()
                .Property(t => t.TickerSymbol)
                .IsRequired()
                .HasMaxLength(10);

            modelBuilder.Entity<Trade>()
                .Property(t => t.BrokerId)
                .IsRequired()
                .HasMaxLength(50);
        }
    }
}
