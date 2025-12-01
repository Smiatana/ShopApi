using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class OrderItemsController : ControllerBase
{
    private readonly ShopContext _context;

    public OrderItemsController(ShopContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet("id")]
    public async Task<ActionResult<OrderItem>> GetOrderItem(int id)
    {
        var item = await _context.OrderItems
            .Include(i => i.Product)
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Id == id);
        
        if (item == null)
        {
            return NotFound();
        }

        if (!AuthHelper.IsOwnerOrAdmin(User, item.Order.UserId))
        {
            return Forbid();
        }
        return item;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<OrderItem>> AddOrderItem(OrderItem item)
    {
        var order = await _context.Orders.FindAsync(item.OrderId);
        if (order == null)
            return BadRequest("Order not found.");

        if (!AuthHelper.IsOwnerOrAdmin(User, order.UserId))
            return Forbid();

        var product = await _context.Products.FindAsync(item.ProductId);
        if (product == null)
            return BadRequest("Product not found.");

        item.PriceAtPurchase = product.Price;

        _context.OrderItems.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrderItem), new { id = item.Id }, item);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrderItem(int id, OrderItem item)
    {
        if (id != item.Id)
            return BadRequest();

        var existing = await _context.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (existing == null)
            return NotFound();

        if (!AuthHelper.IsOwnerOrAdmin(User, existing.Order.UserId))
            return Forbid();

        existing.Quantity = item.Quantity;
        existing.PriceAtPurchase = existing.Product.Price;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        var item = await _context.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
            return NotFound();

        if (!AuthHelper.IsOwnerOrAdmin(User, item.Order.UserId))
            return Forbid();

        _context.OrderItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}