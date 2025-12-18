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
    public async Task<ActionResult<object>> GetComparison(int categoryId)
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var comparison = await _context.Comparisons
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Name == categoryId.ToString());
        
        if (comparison == null)
        {
            return NotFound();
        }

        var products = await _context.Products
            .Where(p => comparison.Products.Contains(p.Id))
            .ToListAsync();
        
        var groupedSpecs = new Dictionary<string, List<object>>();
        foreach (var product in products)
        {
            foreach (var kv in product.Specs)
            {
                if (!groupedSpecs.ContainsKey(kv.Key))
                {
                    groupedSpecs[kv.Key] = new List<object>();
                }
                groupedSpecs[kv.Key].Add(kv.Value);
            }
        }
        return Ok( new
        {
            ComparisonId = comparison.Id,
            CategoryId = categoryId,
            Products = products,
            SpecsComparison = groupedSpecs
        });
    }

    [Authorize]
    [HttpPost("add/{productId}")]
    public async Task<IActionResult> AddProductToComparison(int productId)
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound("Product not found");
        }

        var comparison = await _context.Comparisons
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Name == product.CategoryId.ToString());
        
        if (comparison == null)
        {
            comparison = new Comparison
            {
                UserId = userId,
                Name = product.CategoryId.ToString(),
                Products = new List<int>()
            };
            _context.Comparisons.Add(comparison);
        }

        if (!comparison.Products.Contains(productId))
        {
            comparison.Products.Add(productId);
        }

        await _context.SaveChangesAsync();
        return Ok(comparison);
    }

    [Authorize]
    [HttpDelete("remove/{productId}")]
    public async Task<IActionResult> RemoveProductFromComparison(int productId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return NotFound("Product not found.");

        var comparison = await _context.Comparisons
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Name == product.CategoryId.ToString());

        if (comparison == null)
            return NotFound("Comparison not found.");

        comparison.Products.Remove(productId);

        if (!comparison.Products.Any())
            _context.Comparisons.Remove(comparison);

        await _context.SaveChangesAsync();
        return NoContent();
    }

}