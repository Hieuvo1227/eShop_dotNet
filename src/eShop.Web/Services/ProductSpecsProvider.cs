using eShop.Application.Catalog.DTOs;

namespace eShop.Web.Services;

public class ProductSpecification
{
    public string Screen { get; set; } = "Màn hình tiêu chuẩn";
    public string Cpu { get; set; } = "Vi xử lý thế hệ mới";
    public string Ram { get; set; } = "8 GB";
    public string Storage { get; set; } = "256 GB";
    public string Camera { get; set; } = "Không áp dụng";
    public string Battery { get; set; } = "Thời lượng pin tiêu chuẩn";
    public string Weight { get; set; } = "Tiêu chuẩn";
    public string Os { get; set; } = "Đa nền tảng";
    public string Warranty { get; set; } = "12 tháng chính hãng";
}

public static class ProductSpecsProvider
{
    public static ProductSpecification GetSpecs(ProductDto product)
    {
        var name = product.Name ?? "";

        if (name.Contains("iPhone 15 Pro Max", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductSpecification
            {
                Screen = "6.7 inch Super Retina XDR OLED, 120Hz ProMotion, 2000 nits, Dynamic Island",
                Cpu = "Apple A17 Pro (3nm, 6 nhân CPU, 6 nhân GPU, 16 nhân Neural Engine)",
                Ram = "8 GB LPDDR5",
                Storage = "256 GB NVMe siêu tốc",
                Camera = "Chính 48MP (f/1.78, OIS) + Góc siêu rộng 12MP + Telephoto 5x 12MP",
                Battery = "4.422 mAh, Sạc nhanh 20W (50% trong 30p), Sạc MagSafe không dây 15W",
                Weight = "221 g, Khung vỏ Titan chuẩn hàng không vũ trụ",
                Os = "iOS 17 (Hỗ trợ nâng cấp iOS 18+)",
                Warranty = "12 tháng chính hãng Apple Care Việt Nam (1 đổi 1 trong 30 ngày)"
            };
        }

        if (name.Contains("S24 Ultra", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductSpecification
            {
                Screen = "6.8 inch Dynamic AMOLED 2X, QHD+, 120Hz thích ứng, 2600 nits, Kính Gorilla Armor",
                Cpu = "Snapdragon 8 Gen 3 for Galaxy (4nm, Octa-Core lên đến 3.39GHz)",
                Ram = "12 GB LPDDR5X",
                Storage = "512 GB UFS 4.0",
                Camera = "200MP Chính (OIS) + 50MP Tiềm vọng 5x + 10MP Tele 3x + 12MP Siêu rộng",
                Battery = "5.000 mAh, Sạc siêu nhanh 45W, Bút S-Pen tích hợp trong thân máy",
                Weight = "232 g, Khung viền Titan phẳng cao cấp, Kháng nước IP68",
                Os = "One UI 6.1 (Android 14) tích hợp bộ công cụ Galaxy AI toàn năng",
                Warranty = "12 tháng chính hãng Samsung Việt Nam (Hỗ trợ bảo hành tại nhà)"
            };
        }

        if (name.Contains("MacBook Pro 14", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductSpecification
            {
                Screen = "14.2 inch Liquid Retina XDR (3024 x 1964), 120Hz ProMotion, 1600 nits HDR",
                Cpu = "Apple M3 Pro (11-core CPU, 14-core GPU, Hardware Ray Tracing)",
                Ram = "18 GB Unified Memory (Băng thông 150 GB/s)",
                Storage = "512 GB SSD PCIe tốc độ đọc/ghi 5.000 MB/s",
                Camera = "FaceTime HD 1080p với bộ xử lý tín hiệu hình ảnh điện toán",
                Battery = "Pin 70 Wh, thời lượng lướt web lên đến 18 giờ liên tục, Sạc MagSafe 3 70W",
                Weight = "1.61 kg, Vỏ nhôm nguyên khối tái chế 100% màu Space Black",
                Os = "macOS Sonoma (Tối ưu hóa lập trình và đồ họa chuyên nghiệp)",
                Warranty = "12 tháng chính hãng Apple Việt Nam (Gói hỗ trợ kỹ thuật toàn cầu)"
            };
        }

        if (name.Contains("Dell XPS 15", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductSpecification
            {
                Screen = "15.6 inch OLED 3.5K (3456 x 2160), Cảm ứng đa điểm, 100% DCI-P3, 400 nits",
                Cpu = "Intel Core i7-13700H (14 nhân, 20 luồng, xung nhịp tối đa 5.0 GHz)",
                Ram = "32 GB DDR5 4800MHz (2 khe rời, nâng cấp tối đa 64GB)",
                Storage = "1 TB PCIe Gen4 M.2 NVMe SSD",
                Camera = "720p HD Webcam kèm cảm biến hồng ngoại nhận diện khuôn mặt Windows Hello",
                Battery = "Pin 86 Whr, thời lượng sử dụng 8-10 giờ, Sạc nhanh USB-C 130W",
                Weight = "1.92 kg, Thân máy nhôm CNC phay xước kết hợp sợi Carbon chống bám vân tay",
                Os = "Windows 11 Home bản quyền trọn đời",
                Warranty = "12 tháng bảo hành Dell ProSupport tận nơi (On-site 24/7)"
            };
        }

        if (name.Contains("WH-1000XM5", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductSpecification
            {
                Screen = "Không áp dụng (Thiết bị âm thanh không dây)",
                Cpu = "Bộ xử lý chống ồn tích hợp Sony V1 + Bộ xử lý chống ồn chuyên dụng HD QN1",
                Ram = "Bộ nhớ đệm DSP âm thanh độ phân giải cao Hi-Res Audio",
                Storage = "Không áp dụng",
                Camera = "8 micro chuyên dụng (4 micro mỗi bên) khử ồn chủ động thích ứng",
                Battery = "Pin 30 giờ (bật ANC), Sạc nhanh chuẩn Power Delivery (3 phút nghe 3 giờ)",
                Weight = "250 g, Đệm tai bằng da tổng hợp Soft Fit Leather êm ái",
                Os = "Tương thích hoàn hảo iOS, Android, macOS, Windows (Ứng dụng Sony Headphones)",
                Warranty = "12 tháng chính hãng Sony Việt Nam"
            };
        }

        if (name.Contains("MX Master 3S", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductSpecification
            {
                Screen = "Không áp dụng (Phụ kiện chuột văn phòng)",
                Cpu = "Cảm biến quang học độ chính xác cao Darkfield (Tracking trên mọi bề mặt kể cả kính)",
                Ram = "Lưu trữ 3 cấu hình thiết bị tùy chỉnh qua Logi Options+",
                Storage = "Bộ nhớ tích hợp chuyển đổi nhanh giữa 3 máy tính (Logitech Flow)",
                Camera = "Độ phân giải từ 200 đến 8.000 DPI (chỉnh từng bước 50 DPI)",
                Battery = "Pin sạc Li-Po 500 mAh, sử dụng lên đến 70 ngày cho 1 lần sạc đầy, Sạc nhanh Type-C",
                Weight = "141 g, Thiết kế công thái học tự nhiên bảo vệ cổ tay",
                Os = "Hỗ trợ Windows, macOS, iPadOS, Linux, ChromeOS qua Bluetooth hoặc Logi Bolt",
                Warranty = "12 tháng chính hãng Logitech Việt Nam (Đổi mới trong 12 tháng)"
            };
        }

        // Mặc định thông minh theo Category
        var cat = product.CategoryName?.ToLowerInvariant() ?? "";
        if (cat.Contains("phone") || cat.Contains("thoại"))
        {
            return new ProductSpecification
            {
                Screen = "6.5 inch Full HD+ AMOLED, 90Hz-120Hz mượt mà",
                Cpu = "Vi xử lý 8 nhân hiệu năng cao",
                Ram = "8 GB RAM",
                Storage = "256 GB ROM",
                Camera = "Camera AI sắc nét 50MP + Chụp đêm chuyên nghiệp",
                Battery = "5.000 mAh, Sạc nhanh Type-C",
                Weight = "190 g",
                Os = "Android / iOS",
                Warranty = "12 tháng chính hãng (1 đổi 1 trong 30 ngày)"
            };
        }

        if (cat.Contains("laptop") || cat.Contains("máy tính"))
        {
            return new ProductSpecification
            {
                Screen = "14 - 15.6 inch IPS Full HD chống chói, 100% sRGB",
                Cpu = "Intel Core i5/i7 hoặc AMD Ryzen 7 thế hệ mới",
                Ram = "16 GB DDR5 đa nhiệm mượt mà",
                Storage = "512 GB SSD NVMe siêu tốc",
                Camera = "HD Webcam rõ nét cho học tập và hội họp trực tuyến",
                Battery = "Thời lượng pin 6 - 8 giờ làm việc liên tục",
                Weight = "1.5 kg - 1.8 kg",
                Os = "Windows 11 / macOS",
                Warranty = "12 tháng chính hãng"
            };
        }

        return new ProductSpecification
        {
            Screen = "Tiêu chuẩn phụ kiện",
            Cpu = "Chip xử lý tín hiệu thông minh",
            Ram = "Bộ nhớ điều khiển chuyên dụng",
            Storage = "Không áp dụng",
            Camera = "Không áp dụng",
            Battery = "Thời lượng pin bền bỉ, tiết kiệm năng lượng",
            Weight = "Nhẹ, tiện di chuyển",
            Os = "Tương thích mọi hệ điều hành",
            Warranty = "12 tháng chính hãng 1 đổi 1"
        };
    }
}
