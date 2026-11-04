using eShop.Application.Common.Interfaces;
using eShop.Infrastructure.AI;
using eShop.Infrastructure.AI.Plugins;
using eShop.Infrastructure.AI.Services;
using eShop.Infrastructure.Data;
using eShop.Infrastructure.Data.Interceptors;
using eShop.Infrastructure.EventBus;
using eShop.Infrastructure.EventBus.Consumers;
using eShop.Infrastructure.Identity;
using eShop.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace eShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 1. Đăng ký Interceptor kiểm toán tự động
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        // 2. Lựa chọn Database Provider: "PostgreSQL" hoặc "Sqlite"
        var databaseProvider = configuration["DatabaseProvider"] ?? "Sqlite";

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                var pgConnection = configuration.GetConnectionString("PostgreSqlConnection") 
                    ?? "Host=localhost;Port=5432;Database=eshop_db;Username=postgres;Password=postgres";

                options.UseNpgsql(pgConnection, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                });
            }
            else
            {
                var sqliteConnection = configuration.GetConnectionString("DefaultConnection") 
                    ?? "Data Source=eshop.db";

                options.UseSqlite(sqliteConnection);
            }
        });

        // 3. Ánh xạ Interface IApplicationDbContext -> ApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());

        // 4. Đăng ký Initialiser để nạp dữ liệu mẫu ban đầu
        services.AddScoped<ApplicationDbContextInitialiser>();

        // 5. Cấu hình Giỏ hàng: Redis (hoặc InMemory nếu chưa bật Redis)
        var redisConnection = configuration.GetConnectionString("Redis") 
            ?? configuration["Redis:ConnectionString"] 
            ?? "localhost:6379";
        var useRedis = bool.TryParse(configuration["Redis:Enabled"], out var enabled) && enabled;

        if (useRedis)
        {
            try
            {
                var redis = ConnectionMultiplexer.Connect(redisConnection);
                services.AddSingleton<IConnectionMultiplexer>(redis);
                services.AddScoped<IBasketRepository, RedisBasketRepository>();
            }
            catch (Exception ex)
            {
                var logger = services.BuildServiceProvider().GetService<ILogger<RedisBasketRepository>>();
                logger?.LogWarning(ex, "Không thể kết nối tới Redis Server ({Connection}). Tự động chuyển sang InMemoryBasketRepository dự phòng.", redisConnection);
                services.AddSingleton<IBasketRepository, InMemoryBasketRepository>();
            }
        }
        else
        {
            services.AddSingleton<IBasketRepository, InMemoryBasketRepository>();
        }

        // 6. Cấu hình Event Bus (MassTransit: Hỗ trợ linh hoạt cả RabbitMQ lẫn In-Memory Bus)
        services.AddMassTransit(x =>
        {
            // Đăng ký các Consumer tự động
            x.AddConsumer<OrderNotificationConsumer>();
            x.AddConsumer<OrderStockReservationConsumer>();

            var eventBusProvider = configuration["EventBus:Provider"] ?? "InMemory";

            if (eventBusProvider.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    var host = configuration["EventBus:Host"] ?? "localhost";
                    var username = configuration["EventBus:Username"] ?? "guest";
                    var password = configuration["EventBus:Password"] ?? "guest";

                    cfg.Host(host, "/", h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                // Sử dụng In-Memory Bus cho môi trường local development không cần cài đặt RabbitMQ
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        // 7. Đăng ký IEventBus implementation
        services.AddScoped<IEventBus, MassTransitEventBus>();

        // 8. Cấu hình Trợ lý AI Thông minh (Microsoft Semantic Kernel & Native Plugins)
        services.AddScoped<AiChatTracker>();
        services.AddScoped<ShopCatalogPlugin>();
        services.AddScoped<IChatAssistantService, SemanticKernelAssistantService>();

        // 9. Dịch vụ Xác thực & Phân quyền (JWT & RBAC)
        services.AddSingleton<IAuthService, AuthService>();

        return services;
    }
}
