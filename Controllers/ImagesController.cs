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
