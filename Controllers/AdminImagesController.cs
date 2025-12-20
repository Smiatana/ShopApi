using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

[ApiController]
[Route("api/admin/images")]
public class AdminImagesController : ControllerBase
{
    private readonly ShopContext _context;
    private readonly IWebHostEnvironment _env;

    public AdminImagesController(ShopContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ControlImageDto>>> GetImages()
    {
        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Review || i.OwnerType == OwnerType.User)
            .ToListAsync();

        var result = new List<ControlImageDto>();

        foreach (var i in images)
        {
            string username;

            if (i.OwnerType == OwnerType.User)
            {
                var user = await _context.Users
                    .Where(u => u.Id == i.OwnerId)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync();

                username = user ?? "Unknown";
            }
            else
            {
                var review = await _context.Reviews
                    .Where(r => r.Id == i.OwnerId)
                    .Select(r => r.User.Name)
                    .FirstOrDefaultAsync();

                username = review ?? "Unknown";
            }

            result.Add(new ControlImageDto
            {
                Id = i.Id,
                Url = i.Url,
                AltText = i.AltText,
                OwnerType = i.OwnerType,
                Username = username
            });
        }

        return Ok(result);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var image = await _context.Images.FindAsync(id);
        if (image == null) return NotFound();

        if (!string.IsNullOrEmpty(image.Url))
        {
            var filePath = Path.Combine(_env.WebRootPath, image.Url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        _context.Images.Remove(image);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
