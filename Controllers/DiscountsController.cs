using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DiscountsController : ControllerBase
{
    private readonly ShopContext _context;

    public DiscountsController(ShopContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiscountDto>>> GetDiscounts()
    {
        var discounts = await _context.Discounts
            .Include(d => d.Product)
            .Select(d => new DiscountDto
            {
                Id = d.Id,
                Percentage = d.Percentage,
                ValidFrom = d.ValidFrom,
                ValidTo = d.ValidTo,
                Active = d.Active,
                Product = new ProductNameDto
                {
                    Id = d.Product.Id,
                    Name = d.Product.Name
                }
            })
            .ToListAsync();

        return Ok(discounts);
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<Discount>> GetDiscount(int id)
    {
        var discount = await _context.Discounts
            .Include(d => d.Product)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (discount == null) return NotFound();
        return Ok(discount);
    }
    [HttpPost]
    public async Task<ActionResult<Discount>> CreateDiscount(CreateDiscountRequest request)
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null) return NotFound("Product not found");

        var validFromUtc = DateTime.SpecifyKind(request.ValidFrom, DateTimeKind.Local).ToUniversalTime();
        var validToUtc = DateTime.SpecifyKind(request.ValidTo, DateTimeKind.Local).ToUniversalTime();

        var discount = new Discount
        {
            ProductId = request.ProductId,
            Percentage = request.Percentage,
            ValidFrom = validFromUtc,
            ValidTo = validToUtc,
            Active = true
        };

        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDiscount), new { id = discount.Id }, discount);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDiscount(int id, UpdateDiscountRequest request)
    {
        var discount = await _context.Discounts.FindAsync(id);
        if (discount == null) return NotFound();

        discount.Percentage = request.Percentage;
        discount.ValidFrom = DateTime.SpecifyKind(request.ValidFrom, DateTimeKind.Local).ToUniversalTime();
        discount.ValidTo = DateTime.SpecifyKind(request.ValidTo, DateTimeKind.Local).ToUniversalTime();
        discount.Active = request.Active;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDiscount(int id)
    {
        var discount = await _context.Discounts.FindAsync(id);
        if (discount == null) return NotFound();

        _context.Discounts.Remove(discount);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
