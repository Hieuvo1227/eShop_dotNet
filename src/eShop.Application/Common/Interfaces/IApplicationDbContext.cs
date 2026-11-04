using eShop.Domain.Entities;
using eShop.Domain.Entities.Ordering;
using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Common.Interfaces;

/// <summary>
/// Giao diện trừu tượng hóa DbContext ở tầng Application (Tuân thủ nguyên lý DIP).
/// Tầng Application chỉ gọi các DbSet và SaveChangesAsync qua Interface này,
/// không hề biết gì về cơ sở dữ liệu thật (PostgreSQL hay SQL Server).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Brand> Brands { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
