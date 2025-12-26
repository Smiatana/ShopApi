using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Formats.Asn1;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComparisonsController : ControllerBase
{
    private readonly ShopContext _context;

    public ComparisonsController(ShopContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<string>>> GetMyComparisons()
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var names = await _context.Comparisons
            .Where(c => c.UserId == userId)
            .Select(c => c.Name)
            .ToListAsync();

        return Ok(names);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<ComparisonResponseDto>> GetComparison(string name)
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var comparison = await _context.Comparisons
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.Name == name
            );

        if (comparison == null)
            return NotFound();

        var products = await _context.Products
            .Where(p => comparison.Products.Contains(p.Id))
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();

        var images = await _context.Images
            .Where(i =>
                i.OwnerType == OwnerType.Product &&
                productIds.Contains(i.OwnerId)
            )
            .ToListAsync();

        var productDtos = products.Select(p =>
        {
            var image = images
                .Where(i => i.OwnerId == p.Id)
                .OrderBy(i => i.Position)
                .FirstOrDefault();

            return new ComparisonProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Image = image?.Url,
                Specs = p.Specs ?? new()
            };
        }).ToList();

        var specRows = productDtos
            .SelectMany(p => p.Specs.Keys)
            .Distinct()
            .Select(key => new ComparisonSpecRowDto
            {
                Key = key,
                Values = productDtos
                    .Select(p => p.Specs.TryGetValue(key, out var v) ? v : null)
                    .ToList()
            })
            .ToList();

        return Ok(new ComparisonResponseDto
        {
            CategoryName = comparison.Name,
            Products = productDtos,
            Specs = specRows
        });
    }

    [HttpPost("add/{productId}")]
    public async Task<IActionResult> AddProduct(int productId)
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return NotFound("Product not found");

        var category = await _context.Categories.FindAsync(product.CategoryId);
        if (category == null)
            return NotFound("Category not found");

        var comparison = await _context.Comparisons
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.Name == category.Name
            );

        if (comparison == null)
        {
            comparison = new Comparison
            {
                UserId = userId,
                Name = category.Name,
                Products = new[] { productId }
            };

            _context.Comparisons.Add(comparison);
        }
        else if (!comparison.Products.Contains(productId))
        {
            comparison.Products = comparison.Products
                .Append(productId)
                .ToArray();
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("remove/{productId}")]
    public async Task<IActionResult> RemoveProduct(int productId)
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var comparison = await _context.Comparisons
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.Products.Contains(productId)
            );

        if (comparison == null)
            return NotFound();

        comparison.Products = comparison.Products
            .Where(id => id != productId)
            .ToArray();

        if (comparison.Products.Length == 0)
        {
            _context.Comparisons.Remove(comparison);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
