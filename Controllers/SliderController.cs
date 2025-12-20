using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class SlidersController : ControllerBase
{
    private readonly ShopContext _context;
    private readonly IWebHostEnvironment _env;

    public SlidersController(ShopContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SliderDto>>> GetSliders()
    {
        var sliders = await _context.Sliders.ToListAsync();
        var sliderIds = sliders.Select(s => s.Id).ToList();

        var images = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Slider && sliderIds.Contains(i.OwnerId))
            .ToListAsync();

        return Ok(sliders.Select(s =>
        {
            var image = images.FirstOrDefault(i => i.OwnerId == s.Id);

            return new SliderDto
            {
                Id = s.Id,
                Title = s.Title,
                Subtitle = s.Subtitle,
                Link = s.Link,
                Image = image == null
                    ? null
                    : new ImageDto
                    {
                        Url = image.Url,
                        AltText = image.AltText,
                        Position = 0
                    }
            };
        }));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateSliderRequest request)
    {
        if (request.Image == null || request.Image.Length == 0)
            return BadRequest("Image is required");

        var slider = new Slider
        {
            Title = request.Title,
            Subtitle = request.Subtitle,
            Link = request.Link
        };

        _context.Sliders.Add(slider);
        await _context.SaveChangesAsync();

        var uploads = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploads);

        var fileName = Guid.NewGuid() + Path.GetExtension(request.Image.FileName);
        var path = Path.Combine(uploads, fileName);

        using (var stream = new FileStream(path, FileMode.Create))
        {
            await request.Image.CopyToAsync(stream);
        }

        _context.Images.Add(new Image
        {
            OwnerType = OwnerType.Slider,
            OwnerId = slider.Id,
            Url = $"/uploads/{fileName}",
            Position = 0
        });

        await _context.SaveChangesAsync();
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateSliderRequest request)
    {
        var slider = await _context.Sliders.FindAsync(id);
        if (slider == null) return NotFound();

        slider.Title = request.Title;
        slider.Subtitle = request.Subtitle;
        slider.Link = request.Link;

        if (request.Image != null && request.Image.Length > 0)
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid() + Path.GetExtension(request.Image.FileName);
            var path = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }

            var existingImage = await _context.Images.FirstOrDefaultAsync(i =>
                i.OwnerType == OwnerType.Slider &&
                i.OwnerId == slider.Id);

            if (existingImage != null)
            {
                existingImage.Url = $"/uploads/{fileName}";
            }
            else
            {
                _context.Images.Add(new Image
                {
                    OwnerType = OwnerType.Slider,
                    OwnerId = slider.Id,
                    Url = $"/uploads/{fileName}",
                    Position = 0
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var slider = await _context.Sliders.FindAsync(id);
        if (slider == null) return NotFound();

        var image = await _context.Images.FirstOrDefaultAsync(i =>
            i.OwnerType == OwnerType.Slider &&
            i.OwnerId == id);

        if (image != null)
            _context.Images.Remove(image);

        _context.Sliders.Remove(slider);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
