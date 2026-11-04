using eShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        // Bỏ qua không lưu DomainEvents vào database
        builder.Ignore(p => p.DomainEvents);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.SKU)
            .HasMaxLength(50)
            .IsRequired();

        // Index đảm bảo SKU là duy nhất
        builder.HasIndex(p => p.SKU)
            .IsUnique();

        builder.Property(p => p.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.StockQuantity)
            .IsRequired();

        builder.Property(p => p.PictureUrl)
            .HasMaxLength(500);

        // Index hỗ trợ tìm kiếm và lọc nhanh
        builder.HasIndex(p => p.BrandId);
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.Name);
    }
}

