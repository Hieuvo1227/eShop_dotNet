using eShop.Application.Common.Interfaces;
using eShop.Domain.Entities.Ordering;
using eShop.Domain.Enums;
using eShop.Domain.ValueObjects;
using eShop.Infrastructure.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eShop.UnitTests.Payment;

public class VnPayTestDbContext : DbContext, IApplicationDbContext
{
    public VnPayTestDbContext(DbContextOptions<VnPayTestDbContext> options) : base(options) { }

    public DbSet<eShop.Domain.Entities.Brand> Brands => Set<eShop.Domain.Entities.Brand>();
    public DbSet<eShop.Domain.Entities.Category> Categories => Set<eShop.Domain.Entities.Category>();
    public DbSet<eShop.Domain.Entities.Product> Products => Set<eShop.Domain.Entities.Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().OwnsOne(o => o.ShippingAddress);
        base.OnModelCreating(modelBuilder);
    }
}

public class VnPayServiceTests
{
    private const string SecretKey = "TESTVNPAYSECRETKEY2026TESTDEMO!";
    private readonly IConfiguration _config;

    public VnPayServiceTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VnPay:TmnCode"] = "ESHOPTEST",
                ["VnPay:HashSecret"] = SecretKey,
                ["VnPay:BaseUrl"] = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
                ["VnPay:ReturnUrl"] = "http://localhost:5000/api/payment/vnpay-return"
            })
            .Build();
    }

    private VnPayTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VnPayTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new VnPayTestDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public void CreatePaymentUrl_ShouldGenerateValidUrl_WithHmacSha512Checksum()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new VnPayService(_config, context, NullLogger<VnPayService>.Instance);

        // Act
        var url = service.CreatePaymentUrl(1001, 500000m, "127.0.0.1", "Thanh toan don 1001");

        // Assert
        Assert.NotNull(url);
        Assert.StartsWith("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?", url);
        Assert.Contains("vnp_TmnCode=ESHOPTEST", url);
        Assert.Contains("vnp_Amount=50000000", url); // x100
        Assert.Contains("vnp_TxnRef=1001", url);
        Assert.Contains("vnp_SecureHash=", url);
    }

    [Fact]
    public async Task ProcessIpnAsync_WithValidSignatureAndMatchingAmount_ShouldMarkOrderAsPaid_AndReturnSuccess00()
    {
        // Arrange
        using var context = CreateDbContext();
        var address = new Address("123 Le Loi", "Quan 1", "HCM", "70000", "Vietnam");
        var order = new Order("buyer-test", address, "VNPay");
        order.AddOrderItem(1, "Tai nghe Sony", 500000m, 1, null);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new VnPayService(_config, context, NullLogger<VnPayService>.Instance);

        // Tạo chữ ký hợp lệ
        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Amount", "50000000");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_ResponseCode", "00");
        vnpay.AddRequestData("vnp_TmnCode", "ESHOPTEST");
        vnpay.AddRequestData("vnp_TransactionNo", "14234567");
        vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString());

        var url = vnpay.CreateRequestUrl("http://dummy", SecretKey);
        var uri = new Uri(url);
        var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query)
            .ToDictionary(k => k.Key, v => v.Value.ToString());

        // Act
        var ipnResult = await service.ProcessIpnAsync(queryParams);

        // Assert
        Assert.Equal("00", ipnResult.RspCode);
        Assert.Equal("Confirm Success", ipnResult.Message);

        var updatedOrder = await context.Orders.FindAsync(order.Id);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.Paid, updatedOrder.Status);
        Assert.Equal("14234567", updatedOrder.PaymentTransactionId);
    }

    [Fact]
    public async Task ProcessIpnAsync_WhenCalledTwice_ShouldBeIdempotent_AndReturnCode02()
    {
        // Arrange
        using var context = CreateDbContext();
        var address = new Address("123 Le Loi", "Quan 1", "HCM", "70000", "Vietnam");
        var order = new Order("buyer-test", address, "VNPay");
        order.AddOrderItem(1, "Tai nghe Sony", 500000m, 1, null);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new VnPayService(_config, context, NullLogger<VnPayService>.Instance);

        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Amount", "50000000");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_ResponseCode", "00");
        vnpay.AddRequestData("vnp_TmnCode", "ESHOPTEST");
        vnpay.AddRequestData("vnp_TransactionNo", "14234567");
        vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString());

        var url = vnpay.CreateRequestUrl("http://dummy", SecretKey);
        var uri = new Uri(url);
        var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query)
            .ToDictionary(k => k.Key, v => v.Value.ToString());

        // Act 1: Lần 1 thành công
        var firstCall = await service.ProcessIpnAsync(queryParams);
        Assert.Equal("00", firstCall.RspCode);

        // Act 2: Lần 2 (VNPay retry hoặc lặp lại webhook)
        var secondCall = await service.ProcessIpnAsync(queryParams);

        // Assert (Idempotent: RspCode 02 - Order already confirmed)
        Assert.Equal("02", secondCall.RspCode);
        Assert.Equal("Order already confirmed", secondCall.Message);
    }

    [Fact]
    public async Task ProcessIpnAsync_WithInvalidChecksum_ShouldReturnCode97()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new VnPayService(_config, context, NullLogger<VnPayService>.Instance);

        var queryParams = new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = "1",
            ["vnp_Amount"] = "10000000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_SecureHash"] = "FAKE_TAMPERED_CHECKSUM_HASH_VALUE_HERE"
        };

        // Act
        var result = await service.ProcessIpnAsync(queryParams);

        // Assert
        Assert.Equal("97", result.RspCode);
        Assert.Equal("Invalid Checksum", result.Message);
    }

    [Fact]
    public async Task ProcessIpnAsync_WhenAmountMismatches_ShouldReturnCode04()
    {
        // Arrange
        using var context = CreateDbContext();
        var address = new Address("123 Le Loi", "Quan 1", "HCM", "70000", "Vietnam");
        var order = new Order("buyer-test", address, "VNPay");
        order.AddOrderItem(1, "Tai nghe Sony", 500000m, 1, null); // 500,000 VND
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new VnPayService(_config, context, NullLogger<VnPayService>.Instance);

        // Cố tình tạo giao dịch với số tiền sai lệch (ví dụ 10,000 VND thay vì 500,000 VND)
        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Amount", "1000000"); // 10,000 VND x100
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_ResponseCode", "00");
        vnpay.AddRequestData("vnp_TmnCode", "ESHOPTEST");
        vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString());

        var url = vnpay.CreateRequestUrl("http://dummy", SecretKey);
        var uri = new Uri(url);
        var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query)
            .ToDictionary(k => k.Key, v => v.Value.ToString());

        // Act
        var result = await service.ProcessIpnAsync(queryParams);

        // Assert
        Assert.Equal("04", result.RspCode);
        Assert.Equal("Invalid Amount", result.Message);
    }

    [Fact]
    public async Task ProcessIpnAsync_WhenOrderNotFound_ShouldReturnCode01()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new VnPayService(_config, context, NullLogger<VnPayService>.Instance);

        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Amount", "50000000");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_ResponseCode", "00");
        vnpay.AddRequestData("vnp_TmnCode", "ESHOPTEST");
        vnpay.AddRequestData("vnp_TxnRef", "999999"); // Đơn hàng 999999 không tồn tại

        var url = vnpay.CreateRequestUrl("http://dummy", SecretKey);
        var uri = new Uri(url);
        var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query)
            .ToDictionary(k => k.Key, v => v.Value.ToString());

        // Act
        var result = await service.ProcessIpnAsync(queryParams);

        // Assert
        Assert.Equal("01", result.RspCode);
        Assert.Equal("Order Not Found", result.Message);
    }
}
