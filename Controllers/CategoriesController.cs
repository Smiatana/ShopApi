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
    public async Task<ActionResult<IEnumerable<Category>> >GetCategories()
    {
        return await _context.Categories.ToListAsync();
    }

    [HttpGet("id")]
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
    public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int id)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == id);
        if (!categoryExists)
            return NotFound();

        var products = await _context.Products
            .Where(p => p.CategoryId == id)
            .Include(p => p.Images.OrderBy(i => i.Position))
            .ToListAsync();

        return products;
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