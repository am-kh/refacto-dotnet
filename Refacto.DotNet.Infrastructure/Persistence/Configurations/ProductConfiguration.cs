using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Mapping EF Core de <see cref="Product"/>.
    /// </summary>
    internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            _ = builder.ToTable("products");

            _ = builder.HasKey(p => p.Id);
            _ = builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
            _ = builder.Property(p => p.LeadTime).HasColumnName("lead_time");
            _ = builder.Property(p => p.Available).HasColumnName("available");
            _ = builder.Property(p => p.Type).HasColumnName("type");
            _ = builder.Property(p => p.Name).HasColumnName("name");
            _ = builder.Property(p => p.ExpiryDate).HasColumnName("expiry_date");
            _ = builder.Property(p => p.SeasonStartDate).HasColumnName("season_start_date");
            _ = builder.Property(p => p.SeasonEndDate).HasColumnName("season_end_date");
        }
    }
}
