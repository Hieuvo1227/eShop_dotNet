using eShop.Domain.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        // Bỏ qua danh sách DomainEvents nội bộ
        builder.Ignore(o => o.DomainEvents);

        builder.Property(o => o.BuyerId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.PaymentMethod)
            .HasMaxLength(50)
            .HasDefaultValue("COD")
            .IsRequired();

        builder.Property(o => o.PaymentTransactionId)
            .HasMaxLength(100);

        // Cấu hình Value Object ShippingAddress dưới dạng Owned Entity (DDD Mapping)
        builder.OwnsOne(o => o.ShippingAddress, a =>
        {
            a.Property(p => p.Street).HasMaxLength(200).IsRequired();
            a.Property(p => p.District).HasMaxLength(100).IsRequired();
            a.Property(p => p.City).HasMaxLength(100).IsRequired();
            a.Property(p => p.ZipCode).HasMaxLength(20);
            a.Property(p => p.Country).HasMaxLength(100).IsRequired();
        });

        // Mối quan hệ 1 - N với OrderItems
        builder.HasMany(o => o.OrderItems)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index tìm kiếm theo BuyerId và ngày tạo
        builder.HasIndex(o => o.BuyerId);
        builder.HasIndex(o => o.CreatedAtUtc);
    }
}

