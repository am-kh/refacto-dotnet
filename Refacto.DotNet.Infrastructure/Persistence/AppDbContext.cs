using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Infrastructure.Persistence
{
    /// <summary>
    /// Contexte EF Core de l'application.
    /// </summary>
    internal sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders => Set<Order>();

        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
