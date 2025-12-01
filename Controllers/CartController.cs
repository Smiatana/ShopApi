using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Formats.Asn1;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ShopContext _context;

    public CartController(ShopContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<Cart>> GetCart()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            cart = new Cart { UserId = userId, Items = new List<CartItem>() };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

        return cart;
    }

    [Authorize]
    [HttpPost("items")]
    public async Task<ActionResult<CartItem>> AddItem(int productId, int quantity)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId, Items = new List<CartItem>() };
            _context.Carts.Add(cart);
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return BadRequest("Product not found");
        }
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = productId,
                Quantity = quantity
            });
        }
        await _context.SaveChangesAsync();

        return Ok(cart.Items);
    }

    [Authorize]
    [HttpPut("items/{itemId}")]
    public async Task<IActionResult> UpdateItem(int itemId, int quantity)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var item = await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Cart.UserId == userId);

        if (item == null)
            return NotFound();

        item.Quantity = quantity;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpDelete("items/{itemId}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var item = await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Cart.UserId == userId);

        if (item == null)
            return NotFound();

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return NotFound();

        _context.CartItems.RemoveRange(cart.Items);
        await _context.SaveChangesAsync();

        return NoContent();
    }


}