using System.Security.Claims;
using System.Text;
using eShop.API.Endpoints;
using eShop.API.Infrastructure;
using eShop.Application;
using eShop.Infrastructure;
using eShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các tầng dịch vụ (Clean Architecture)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Cấu hình xử lý Exception tập trung chuẩn .NET 8 RFC 7807
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. Cấu hình Authentication & Authorization với JWT Bearer & RBAC
var jwtSecret = builder.Configuration["JwtSettings:Secret"] 
    ?? "eShop_CleanArchitecture_Super_Secret_Key_For_JWT_Authentication_2026_DotNet8!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "eShop.API";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "eShop.Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        RoleClaimType = ClaimTypes.Role
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
});

// 4. Swagger / OpenAPI Documentation có tích hợp nút nhập JWT Bearer Token
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "eShop Clean Architecture API",
        Version = "v1",
        Description = "Nền tảng eCommerce thực chiến kết hợp CQRS, Redis, PostgreSQL, AI Semantic Kernel và JWT RBAC Security."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT Bearer token theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Khởi tạo và nạp dữ liệu mẫu cho Database (nếu kết nối được)
using (var scope = app.Services.CreateScope())
{
    var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
    await initialiser.InitialiseAsync();
    await initialiser.SeedAsync();
}

// Cấu hình HTTP request pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "eShop API v1");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Endpoint kiểm tra sức khỏe hệ thống (Health Check)
app.MapGet("/", () => Results.Ok(new
{
    Application = "eShop Web API",
    Status = "Healthy",
    Version = "1.0.0",
    Timestamp = DateTime.UtcNow
}))
.WithName("HealthCheck")
.WithOpenApi();

// Đăng ký toàn bộ Endpoint của Auth, Catalog, Basket, Ordering và AI Assistant Module
app.MapAuthEndpoints();
app.MapCatalogEndpoints();
app.MapBasketEndpoints();
app.MapOrderEndpoints();
app.MapAiEndpoints();

app.Run();
