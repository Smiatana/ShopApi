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
    public async Task<IActionResult> GetCart()
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var cart = await _context.Carts
        .Where(c => c.UserId == userId)
        .Include(c => c.Items)
            .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Discounts)
        .ToListAsync();


        var productIds = cart.SelectMany(c => c.Items.Select(i => i.ProductId)).ToList();

        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Product && productIds.Contains(i.OwnerId))
            .ToListAsync();

        var now = DateTime.UtcNow;

        var cartDto = cart.Select(c => new
        {
            c.Id,
            Items = c.Items.Select(i => new
            {
                i.Id,
                i.ProductId,
                i.Quantity,
                Product = ProductMapper.ToListDto(i.Product, images, now)
            })
        }).FirstOrDefault();

        if (cartDto == null)
        {
            var newCart = new Cart { UserId = userId };
            _context.Carts.Add(newCart);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                newCart.Id,
                Items = Array.Empty<object>()
            });
        }

        return Ok(cartDto);
    }


    [Authorize]
    [HttpPost("items")]
    public async Task<ActionResult<CartItem>> AddItem(int productId, int quantity)
    {
        var userId = AuthHelper.GetUserId(User, _context);

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

        return Ok(cart.Items.Select(i => new
        {
            i.Id,
            i.ProductId,
            i.Quantity
        }));
    }

    [Authorize]
    [HttpPut("items/{itemId}")]
    public async Task<IActionResult> UpdateItem(int itemId, int quantity)
    {
        var userId = AuthHelper.GetUserId(User, _context);

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
        var userId = AuthHelper.GetUserId(User, _context);

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
        var userId = AuthHelper.GetUserId(User, _context);

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