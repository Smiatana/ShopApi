using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ShopContext _context;

    public OrdersController(ShopContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

       if (!AuthHelper.IsOwnerOrAdmin(User, order.UserId))
            return Forbid();

        return order;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(Order order)
    {
        var userId = AuthHelper.GetUserId(User);

        order.UserId = userId;
        order.Status = OrderStatus.Pending;
        order.CreatedAt = DateTime.UtcNow;

        decimal total = 0;

        foreach (var item in order.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
            {
                return BadRequest($"Product {item.ProductId} not found");
            }
            item.PriceAtPurchase = product.Price;
            total += product.Price * item.Quantity;
        }
        order.TotalPrice = total;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new {id = order.Id}, order);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return NotFound();

        order.Status = status;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return NotFound();

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}