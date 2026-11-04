using eShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eShop.Infrastructure.Data;

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger, 
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            if (_context.Database.IsRelational())
            {
                await _context.Database.MigrateAsync();
            }
            else
            {
                await _context.Database.EnsureCreatedAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đã xảy ra lỗi khi khởi tạo cơ sở dữ liệu: {Message}", ex.Message);
            // Không rethrow để ứng dụng vẫn có thể chạy và hiển thị thông tin hướng dẫn
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đã xảy ra lỗi khi nạp dữ liệu mẫu ban đầu: {Message}", ex.Message);
        }
    }

    private async Task TrySeedAsync()
    {
        // 1. Nạp Thương hiệu mẫu (Brands)
        if (!await _context.Brands.AnyAsync())
        {
            var brands = new List<Brand>
            {
                new() { Name = "Apple", Description = "Thương hiệu công nghệ hàng đầu thế giới từ Mỹ" },
                new() { Name = "Samsung", Description = "Tập đoàn công nghệ điện tử hàng đầu Hàn Quốc" },
                new() { Name = "Sony", Description = "Thương hiệu âm thanh và hình ảnh cao cấp từ Nhật Bản" },
                new() { Name = "Dell", Description = "Thương hiệu máy tính xách tay và máy trạm chuyên nghiệp" },
                new() { Name = "Logitech", Description = "Thương hiệu phụ kiện máy tính và thiết bị ngoại vi Thụy Sĩ" }
            };

            await _context.Brands.AddRangeAsync(brands);
            await _context.SaveChangesAsync(CancellationToken.None);
        }

        // 2. Nạp Danh mục mẫu (Categories)
        if (!await _context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new() { Name = "Smartphones", Description = "Điện thoại thông minh và thiết bị cầm tay" },
                new() { Name = "Laptops", Description = "Máy tính xách tay văn phòng và gaming" },
                new() { Name = "Phụ kiện", Description = "Tai nghe, bàn phím, chuột và củ sạc nhanh" },
                new() { Name = "Âm thanh", Description = "Loa bluetooth, dàn âm thanh gia đình" }
            };

            await _context.Categories.AddRangeAsync(categories);
            await _context.SaveChangesAsync(CancellationToken.None);
        }

        // 3. Nạp Sản phẩm mẫu (Products)
        if (!await _context.Products.AnyAsync())
        {
            var apple = await _context.Brands.FirstAsync(b => b.Name == "Apple");
            var samsung = await _context.Brands.FirstAsync(b => b.Name == "Samsung");
            var sony = await _context.Brands.FirstAsync(b => b.Name == "Sony");
            var dell = await _context.Brands.FirstAsync(b => b.Name == "Dell");
            var logitech = await _context.Brands.FirstAsync(b => b.Name == "Logitech");

            var phones = await _context.Categories.FirstAsync(c => c.Name == "Smartphones");
            var laptops = await _context.Categories.FirstAsync(c => c.Name == "Laptops");
            var accessories = await _context.Categories.FirstAsync(c => c.Name == "Phụ kiện");
            var audio = await _context.Categories.FirstAsync(c => c.Name == "Âm thanh");

            var products = new List<Product>
            {
                new()
                {
                    Name = "iPhone 15 Pro Max 256GB",
                    Description = "Thiết kế khung titan sang trọng, chip A17 Pro mạnh mẽ, camera tiềm vọng 5x.",
                    SKU = "IP15PM-256",
                    Price = 29990000m,
                    StockQuantity = 45,
                    PictureUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=500",
                    BrandId = apple.Id,
                    CategoryId = phones.Id
                },
                new()
                {
                    Name = "Samsung Galaxy S24 Ultra 512GB",
                    Description = "Quyền năng Galaxy AI, camera 200MP, bút S-Pen tích hợp, khung viền titan bền bỉ.",
                    SKU = "SS-S24U-512",
                    Price = 28490000m,
                    StockQuantity = 30,
                    PictureUrl = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?w=500",
                    BrandId = samsung.Id,
                    CategoryId = phones.Id
                },
                new()
                {
                    Name = "MacBook Pro 14 M3 Pro",
                    Description = "Chip Apple M3 Pro 11-core CPU, 14-core GPU, màn hình Liquid Retina XDR 120Hz sắc nét.",
                    SKU = "MBP14-M3PRO",
                    Price = 49990000m,
                    StockQuantity = 18,
                    PictureUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=500",
                    BrandId = apple.Id,
                    CategoryId = laptops.Id
                },
                new()
                {
                    Name = "Dell XPS 15 9530",
                    Description = "Intel Core i7-13700H, RAM 32GB, SSD 1TB, card đồ họa RTX 4060, màn hình cảm ứng OLED 3.5K.",
                    SKU = "DELL-XPS15-9530",
                    Price = 45500000m,
                    StockQuantity = 12,
                    PictureUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=500",
                    BrandId = dell.Id,
                    CategoryId = laptops.Id
                },
                new()
                {
                    Name = "Tai nghe Sony WH-1000XM5",
                    Description = "Công nghệ chống ồn đỉnh cao số 1 thế giới với bộ xử lý V1 và QN1, thời lượng pin 30 giờ.",
                    SKU = "SONY-WH1000XM5",
                    Price = 7990000m,
                    StockQuantity = 60,
                    PictureUrl = "https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=500",
                    BrandId = sony.Id,
                    CategoryId = audio.Id
                },
                new()
                {
                    Name = "Chuột không dây Logitech MX Master 3S",
                    Description = "Cảm biến 8000 DPI lướt mượt trên kính, con lăn MagSpeed siêu tốc, bấm êm Quiet Clicks.",
                    SKU = "LOGI-MXM3S",
                    Price = 2490000m,
                    StockQuantity = 120,
                    PictureUrl = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=500",
                    BrandId = logitech.Id,
                    CategoryId = accessories.Id
                }
            };

            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync(CancellationToken.None);
        }
    }
}

