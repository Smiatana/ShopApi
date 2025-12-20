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

    [Authorize(Roles = "Admin")]
    [HttpGet("/api/admin/orders")]
    public async Task<ActionResult<IEnumerable<OrderReadDto>>> GetAllOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var productIds = orders.SelectMany(o => o.Items.Select(i => i.ProductId)).ToList();
        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Product && productIds.Contains(i.OwnerId))
            .ToListAsync();

        return Ok(orders.Select(o => new OrderReadDto
        {
            Id = o.Id,
            TotalPrice = o.TotalPrice,
            Status = o.Status.ToString(),
            CreatedAt = o.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            Items = o.Items.Select(i =>
            {
                var img = images
                    .Where(im => im.OwnerId == i.ProductId)
                    .OrderBy(im => im.Position)
                    .FirstOrDefault();

                return new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Name = i.Product.Name,
                    Brand = i.Product.Brand,
                    PriceAtPurchase = i.PriceAtPurchase,
                    Quantity = i.Quantity,
                    FirstImage = img == null ? null : new ImageDto
                    {
                        Url = img.Url,
                        AltText = img.AltText,
                        Position = img.Position
                    }
                };
            }).ToList()
        }));
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderReadDto>>> GetOrders()
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .ToListAsync();

        var productIds = orders.SelectMany(o => o.Items.Select(i => i.ProductId)).ToList();
        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Product && productIds.Contains(i.OwnerId))
            .ToListAsync();

        var orderDtos = orders.Select(o => new OrderReadDto
        {
            Id = o.Id,
            TotalPrice = o.TotalPrice,
            Status = o.Status.ToString(),
            CreatedAt = o.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            Items = o.Items.Select(i =>
            {
                var firstImage = images
                    .Where(img => img.OwnerId == i.ProductId)
                    .OrderBy(img => img.Position)
                    .FirstOrDefault();

                return new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Name = i.Product.Name,
                    Brand = i.Product.Brand,
                    PriceAtPurchase = i.PriceAtPurchase,
                    Quantity = i.Quantity,
                    FirstImage = firstImage != null ? new ImageDto
                    {
                        Url = firstImage.Url,
                        AltText = firstImage.AltText,
                        Position = firstImage.Position
                    } : null
                };
            }).ToList()
        }).ToList();

        return Ok(orderDtos);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderReadDto>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        if (!AuthHelper.IsOwnerOrAdmin(User, order.UserId)) return Forbid();

        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Product && productIds.Contains(i.OwnerId))
            .ToListAsync();

        var orderDto = new OrderReadDto
        {
            Id = order.Id,
            TotalPrice = order.TotalPrice,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            Items = order.Items.Select(i =>
            {
                var firstImage = images
                    .Where(img => img.OwnerId == i.ProductId)
                    .OrderBy(img => img.Position)
                    .FirstOrDefault();

                return new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Name = i.Product.Name,
                    Brand = i.Product.Brand,
                    PriceAtPurchase = i.PriceAtPurchase,
                    Quantity = i.Quantity,
                    FirstImage = firstImage != null ? new ImageDto
                    {
                        Url = firstImage.Url,
                        AltText = firstImage.AltText,
                        Position = firstImage.Position
                    } : null
                };
            }).ToList()
        };

        return Ok(orderDto);
    }


    [Authorize]
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var userId = AuthHelper.GetUserId(User, _context);

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        decimal total = 0m;

        foreach (var item in request.Items)
        {
            var product = await _context.Products
                .Include(p => p.Discounts)
                .FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product == null)
                return BadRequest($"Product {item.ProductId} not found");

            var now = DateTime.UtcNow;
            var activeDiscount = product.Discounts?
                .Where(d => d.Active && d.ValidFrom <= now && d.ValidTo >= now)
                .OrderByDescending(d => d.Percentage)
                .FirstOrDefault();

            var discountedPrice = activeDiscount != null
                ? product.Price * (1 - activeDiscount.Percentage / 100)
                : product.Price;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                PriceAtPurchase = discountedPrice
            });

            total += discountedPrice * item.Quantity;
        }

        order.TotalPrice = total;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Product && productIds.Contains(i.OwnerId))
            .ToListAsync();

        var orderDto = new OrderDto
        {
            Id = order.Id,
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i =>
            {
                var firstImage = images
                    .Where(img => img.OwnerId == i.ProductId)
                    .OrderBy(img => img.Position)
                    .FirstOrDefault();

                return new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Name = i.Product.Name,
                    Brand = i.Product.Brand,
                    PriceAtPurchase = i.PriceAtPurchase,
                    Quantity = i.Quantity,
                    FirstImage = firstImage != null ? new ImageDto
                    {
                        Url = firstImage.Url,
                        AltText = firstImage.AltText,
                        Position = firstImage.Position
                    } : null
                };
            }).ToList()
        };

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderDto);
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