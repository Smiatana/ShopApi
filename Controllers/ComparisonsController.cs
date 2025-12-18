using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Formats.Asn1;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class ComparisonsController : ControllerBase
{
    private readonly ShopContext _context;

    public ComparisonsController(ShopContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<ComparisonResponseDto>> GetComparison(int categoryId)
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var comparison = await _context.Comparisons
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Name == categoryId.ToString());

        if (comparison == null)
            return NotFound();

        var products = await _context.Products
            .Where(p => comparison.Products.Contains(p.Id))
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();

        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Product && productIds.Contains(i.OwnerId))
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

        var allSpecKeys = productDtos
            .SelectMany(p => p.Specs.Keys)
            .Distinct()
            .ToList();

        var specRows = allSpecKeys.Select(key =>
            new ComparisonSpecRowDto
            {
                Key = key,
                Values = productDtos
                    .Select(p => p.Specs.TryGetValue(key, out var v) ? v : null)
                    .ToList()
            }
        ).ToList();

        return Ok(new ComparisonResponseDto
        {
            CategoryId = categoryId,
            Products = productDtos,
            Specs = specRows
        });
    }


    [Authorize]
    [HttpPost("add/{productId}")]
    public async Task<IActionResult> AddProductToComparison(int productId)
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return NotFound("Product not found");

        var comparison = await _context.Comparisons
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.Name == product.CategoryId.ToString()
            );

        if (comparison == null)
        {
            comparison = new Comparison
            {
                UserId = userId,
                Name = product.CategoryId.ToString(),
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


    [Authorize]
    [HttpDelete("remove/{productId}")]
    public async Task<IActionResult> RemoveProductFromComparison(int productId)
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var comparison = await _context.Comparisons
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.Products.Contains(productId)
            );

        if (comparison == null)
            return NotFound();

        // ✅ remove element from array
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