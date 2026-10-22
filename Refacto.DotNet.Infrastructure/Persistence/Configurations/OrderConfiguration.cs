using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Mapping EF Core de <see cref="Order"/>.
    /// </summary>
    internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            _ = builder.ToTable("orders");

            _ = builder.HasKey(o => o.Id);
            _ = builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedOnAdd();

            // Relation many-to-many : Product n'expose pas de collection d'Order.
            _ = builder.HasMany(o => o.Items).WithMany();
        }
    }
}
