using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ShopContext _context;

    public CategoriesController(ShopContext context)
    {
        _context = context;
    }

#region GET

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetCategories()
    {
        var categories = await _context.Categories.ToListAsync();
        var categoryIds = categories.Select(c => c.Id).ToList();

        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Category && categoryIds.Contains(i.OwnerId))
            .ToListAsync();

        return categories.Select(c => new
        {
            c.Id,
            c.Name,
            c.Description,
            Image = images
                .Where(i => i.OwnerId == c.Id)
                .OrderBy(i => i.Position)
                .Select(i => i.Url)
                .FirstOrDefault()
        }).ToList();
    }

    [HttpGet("{id}")]

    public async Task<ActionResult<Category>> GetCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        return category;
    }

    [HttpGet("{id}/products")]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProductsByCategory(int id)
    {
        if (!await _context.Categories.AnyAsync(c => c.Id == id))
            return NotFound();

        var products = await _context.Products
            .Where(p => p.CategoryId == id)
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


#endregion
#region POST

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("with-image")]
    public async Task<ActionResult<Category>> CreateCategoryWithImage(
        [FromForm] CreateCategoryRequest request,
        [FromServices] IWebHostEnvironment env)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        if (request.Image != null && request.Image.Length > 0)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(request.Image.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }

            var image = new Image
            {
                OwnerId = category.Id,
                OwnerType = OwnerType.Category,
                Url = $"/uploads/{fileName}",
                AltText = request.AltText ?? string.Empty,
                Position = 0,

                Category = category,
                Product = null,
                Review = null,
                User = null
            };

            _context.Images.Add(image);
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }


#endregion
#region PUT

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, Category category)
    {
        if (id != category.Id)
            return BadRequest();

        _context.Entry(category).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Categories.Any(e => e.Id == id))
                return NotFound();
            else
                throw;
        }

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/with-image")]
    public async Task<IActionResult> UpdateCategoryWithImage(
        int id,
        [FromForm] CreateCategoryRequest request,
        [FromServices] IWebHostEnvironment env)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();

        category.Name = request.Name;
        category.Description = request.Description;

        if (request.Image != null && request.Image.Length > 0)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(request.Image.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }

            var existingImage = await _context.Images.FirstOrDefaultAsync(i =>
                i.OwnerType == OwnerType.Category &&
                i.OwnerId == category.Id &&
                i.Position == 0
            );

            if (existingImage != null)
            {
                existingImage.Url = $"/uploads/{fileName}";
                existingImage.AltText = request.AltText ?? category.Name;
            }
            else
            {
                _context.Images.Add(new Image
                {
                    OwnerId = category.Id,
                    OwnerType = OwnerType.Category,
                    Url = $"/uploads/{fileName}",
                    AltText = request.AltText ?? category.Name,
                    Position = 0
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }


#endregion
#region DELETE

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }

#endregion
}