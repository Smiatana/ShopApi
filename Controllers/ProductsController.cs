using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ShopContext _context;

    public ProductsController(ShopContext context)
    {
        _context = context;
    }

    #region GET

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProducts()
    {
        var products = await _context.Products
            .Include(p => p.Discounts)
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();

        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Product && productIds.Contains(i.OwnerId))
            .ToListAsync();

        var now = DateTime.UtcNow;

        return products
            .Select(p => ProductMapper.ToListDto(p, images, now))
            .ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDetailsDto>> GetProduct(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Product && i.OwnerId == id)
            .OrderBy(i => i.Position)
            .ToListAsync();

        return ProductMapper.ToDetailsDto(product, images);
    }


    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> SearchProducts(
        [FromQuery] string? query,
        [FromQuery] int? categoryId)
    {
        IQueryable<Product> q = _context.Products
            .Include(p => p.Discounts);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";

            q = q.Where(p =>
                EF.Functions.Like(p.Name, pattern) ||
                EF.Functions.Like(p.Brand, pattern) ||
                EF.Functions.Like(p.Description, pattern));
        }

        if (categoryId.HasValue)
        {
            q = q.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await q.ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();

        var images = await _context.Images
            .Where(i =>
                i.OwnerType == OwnerType.Product &&
                productIds.Contains(i.OwnerId))
            .ToListAsync();

        var now = DateTime.UtcNow;

        return Ok(products.Select(p =>
            ProductMapper.ToListDto(p, images, now)));
    }




    [HttpGet("discounted")]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetDiscountedProducts()
    {
        var now = DateTime.UtcNow;

        var products = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Discounts)
            .Where(p => p.Discounts.Any(d => d.Active && d.ValidFrom <= now && d.ValidTo >= now))
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();

        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Product && productIds.Contains(i.OwnerId))
            .ToListAsync();

        return products.Select(p => ProductMapper.ToListDto(p, images, now)).ToList();
    }

    #endregion

    #region POST

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("with-images")]
    public async Task<ActionResult<Product>> CreateProductWithImages(
        [FromForm] CreateProductRequest request,
        [FromServices] IWebHostEnvironment env)
    {
        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Brand = request.Brand,
            Price = request.Price,
            Description = request.Description,
            Specs = request.Specs ?? new Dictionary<string, object>(),
            StockQuantity = request.StockQuantity
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        if (request.Images != null && request.Images.Count > 0)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            int position = 0;
            foreach (var file in request.Images)
            {
                if (file == null || file.Length == 0) continue;

                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                _context.Images.Add(new Image
                {
                    OwnerId = product.Id,
                    OwnerType = OwnerType.Product,
                    Url = $"/uploads/{fileName}",
                    AltText = product.Name,
                    Position = position++,
                    Product = product
                });
            }

            await _context.SaveChangesAsync();
        }

        var created = await _context.Products
            .Include(p => p.Images.OrderBy(i => i.Position))
            .FirstAsync(p => p.Id == product.Id);

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, created);
    }

    #endregion

    #region PUT

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, Product product)
    {
        if (id != product.Id)
            return BadRequest();

        _context.Entry(product).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Products.Any(e => e.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/with-images")]
    public async Task<IActionResult> UpdateProductWithImages(
        int id,
        [FromForm] UpdateProductRequest request,
        [FromServices] IWebHostEnvironment env)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        product.Name = request.Name;
        product.Brand = request.Brand;
        product.Price = request.Price;
        product.Description = request.Description;

        product.Specs = string.IsNullOrWhiteSpace(request.Specs)
            ? product.Specs
            : JsonSerializer.Deserialize<Dictionary<string, object>>(request.Specs);

        product.StockQuantity = request.StockQuantity;
        product.CategoryId = request.CategoryId == 0 ? product.CategoryId : request.CategoryId;

        if (request.NewImages != null && request.NewImages.Count > 0)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var currentMaxPosition = await _context.Images
                .Where(i => i.OwnerId == product.Id && i.OwnerType == OwnerType.Product)
                .MaxAsync(i => (int?)i.Position) ?? -1;

            int position = currentMaxPosition + 1;
            foreach (var file in request.NewImages)
            {
                if (file == null || file.Length == 0) continue;

                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                _context.Images.Add(new Image
                {
                    OwnerId = product.Id,
                    OwnerType = OwnerType.Product,
                    Url = $"/uploads/{fileName}",
                    AltText = product.Name,
                    Position = position++,
                    Product = product
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/move")]
    public async Task<IActionResult> MoveProductToCategory(int id, [FromQuery] int newCategoryId)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound("Product not found.");

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == newCategoryId);
        if (!categoryExists)
            return NotFound("Target category not found.");

        product.CategoryId = newCategoryId;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    #endregion

    #region DELETE

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    #endregion
}
