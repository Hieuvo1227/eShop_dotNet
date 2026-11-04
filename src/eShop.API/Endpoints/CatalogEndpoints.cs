using eShop.Application.Catalog.Commands.CreateProduct;
using eShop.Application.Catalog.Commands.UpdateProductStock;
using eShop.Application.Catalog.DTOs;
using eShop.Application.Catalog.Queries.GetBrands;
using eShop.Application.Catalog.Queries.GetCategories;
using eShop.Application.Catalog.Queries.GetProductById;
using eShop.Application.Catalog.Queries.GetProductsWithPagination;
using eShop.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eShop.API.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog")
            .WithTags("Catalog - Danh mục sản phẩm");

        // 1. GET /api/catalog/products - Lấy danh sách sản phẩm phân trang & tìm kiếm
        group.MapGet("/products", async (
            [AsParameters] GetProductsWithPaginationQuery query, 
            ISender sender) =>
        {
            var result = await sender.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetProducts")
        .WithSummary("Lấy danh sách sản phẩm có phân trang, lọc theo Brand, Category, tìm kiếm từ khóa và sắp xếp");

        // 2. GET /api/catalog/products/{id} - Lấy chi tiết một sản phẩm
        group.MapGet("/products/{id:int}", async (int id, ISender sender) =>
        {
            var product = await sender.Send(new GetProductByIdQuery(id));
            return Results.Ok(product);
        })
        .WithName("GetProductById")
        .WithSummary("Lấy thông tin chi tiết một sản phẩm theo Id");

        // 3. POST /api/catalog/products - Tạo mới sản phẩm (Yêu cầu quyền Admin)
        group.MapPost("/products", async ([FromBody] CreateProductCommand command, ISender sender) =>
        {
            var id = await sender.Send(command);
            return Results.Created($"/api/catalog/products/{id}", new { Id = id });
        })
        .RequireAuthorization("AdminOnly")
        .WithName("CreateProduct")
        .WithSummary("Thêm mới một sản phẩm vào hệ thống (Chỉ dành cho Quản trị viên - Role Admin)");

        // 4. PUT /api/catalog/products/{id}/stock - Điều chỉnh tồn kho (Yêu cầu quyền Admin)
        group.MapPut("/products/{id:int}/stock", async (
            int id, 
            [FromBody] UpdateStockRequest request, 
            ISender sender) =>
        {
            var newStock = await sender.Send(new UpdateProductStockCommand(id, request.Adjustment));
            return Results.Ok(new { ProductId = id, StockQuantity = newStock });
        })
        .RequireAuthorization("AdminOnly")
        .WithName("UpdateProductStock")
        .WithSummary("Tăng hoặc giảm số lượng hàng tồn kho của sản phẩm (Chỉ dành cho Admin)");

        // 5. GET /api/catalog/brands - Lấy danh sách thương hiệu
        group.MapGet("/brands", async (ISender sender) =>
        {
            var brands = await sender.Send(new GetBrandsQuery());
            return Results.Ok(brands);
        })
        .WithName("GetBrands")
        .WithSummary("Lấy danh sách tất cả thương hiệu");

        // 6. GET /api/catalog/categories - Lấy danh sách ngành hàng
        group.MapGet("/categories", async (ISender sender) =>
        {
            var categories = await sender.Send(new GetCategoriesQuery());
            return Results.Ok(categories);
        })
        .WithName("GetCategories")
        .WithSummary("Lấy danh sách tất cả danh mục ngành hàng");

        // Hỗ trợ thêm Route Alias trực tiếp /api/products
        app.MapGet("/api/products", async ([AsParameters] GetProductsWithPaginationQuery query, ISender sender) =>
        {
            var result = await sender.Send(query);
            return Results.Ok(result);
        })
        .WithTags("Catalog - Danh mục sản phẩm");

        app.MapGet("/api/products/{id:int}", async (int id, ISender sender) =>
        {
            var product = await sender.Send(new GetProductByIdQuery(id));
            return Results.Ok(product);
        })
        .WithTags("Catalog - Danh mục sản phẩm");

        return app;
    }
}

public record UpdateStockRequest(int Adjustment);

