using eShop.Application.Catalog.DTOs;
using eShop.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Catalog.Queries.GetBrands;

public record GetBrandsQuery : IRequest<List<BrandDto>>;

public class GetBrandsQueryHandler : IRequestHandler<GetBrandsQuery, List<BrandDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBrandsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BrandDto>> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BrandDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description
            })
            .ToListAsync(cancellationToken);
    }
}

