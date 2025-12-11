using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly ShopContext _context;
    private readonly IWebHostEnvironment _env;

    public ImagesController(ShopContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [Authorize]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(IFormFile file, int ownerId, OwnerType ownerType, string? altText = null, int position = 0)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var image = new Image
        {
            OwnerId = ownerId,
            OwnerType = ownerType,
            Url = $"/uploads/{fileName}",
            AltText = altText ?? string.Empty,
            Position = position
        };

        if (ownerType == OwnerType.Product)
        {
            var product = await _context.Products.FindAsync(ownerId);
            if (product != null)
                image.Product = product;
        }
        else if (ownerType == OwnerType.Category)
        {
            var category = await _context.Categories.FindAsync(ownerId);
            if (category != null)
                image.Category = category;
        }
        else if (ownerType == OwnerType.Review)
        {
            var review = await _context.Reviews.FindAsync(ownerId);
            if (review != null)
                image.Review = review;
        }
        else if (ownerType == OwnerType.User)
        {
            var user = await _context.Users.FindAsync(ownerId);
            if (user != null)
                image.User = user;
        }

        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        return Ok(image);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderImages([FromBody] UpdateImageOrderRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest("No items provided.");

        var imageIds = request.Items.Select(i => i.ImageId).ToList();
        var images = await _context.Images
            .Where(i => imageIds.Contains(i.Id))
            .ToListAsync();

        var dict = request.Items.ToDictionary(i => i.ImageId, i => i.Position);

        foreach (var img in images)
        {
            if (dict.TryGetValue(img.Id, out var pos))
                img.Position = pos;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("by-owner")]
    public async Task<ActionResult<IEnumerable<Image>>> GetImages(int ownerId, OwnerType ownerType)
    {
        return await _context.Images
            .Where(i => i.OwnerId == ownerId && i.OwnerType == ownerType)
            .ToListAsync();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var image = await _context.Images.FindAsync(id);
        if (image == null)
            return NotFound();

        _context.Images.Remove(image);
        await _context.SaveChangesAsync();

        return NoContent();
    }

}
