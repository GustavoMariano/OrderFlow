using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Postgres.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("order_items");
        b.HasKey(x => x.Id);

        b.Property<Guid>("OrderId").IsRequired();

        b.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();

        b.Property(x => x.Quantity).IsRequired();
        b.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)").IsRequired();

        b.Ignore(x => x.LineTotal);
        b.Ignore(x => x.DomainEvents);
    }
}
