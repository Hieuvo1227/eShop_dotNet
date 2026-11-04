using eShop.Web.Components;
using eShop.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký Blazor Interactive Server Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. Đăng ký các dịch vụ trạng thái phía Client
builder.Services.AddScoped<CartStateService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<AuthenticationDelegatingHandler>();

// 3. Cấu hình HttpClient kết nối tới eShop.API qua DelegatingHandler bảo mật Token
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";
builder.Services.AddHttpClient<ShopApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
