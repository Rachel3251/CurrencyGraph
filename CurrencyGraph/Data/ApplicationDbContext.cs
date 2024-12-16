namespace CurrencyGraph.Data
{
    using CurrencyGraph.Models;
    using Microsoft.EntityFrameworkCore;

    public class ApplicationDbContext : DbContext
    {
        public DbSet<Currency_graph> CurrencyGraphs { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Currency_graph>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Currency_graph>()
                .Property(c => c.CurrencyCode)
                .HasMaxLength(3);

            modelBuilder.Entity<Currency_graph>()
                .Property(c => c.CurrencyName)
                .HasMaxLength(50);

            modelBuilder.Entity<Currency_graph>()
                .Property(c => c.ExchangeRate)
                .IsRequired();

            modelBuilder.Entity<Currency_graph>()
                .Property(c => c.CurrencyName)
                .IsRequired();
        }
    }
}
