using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ShopContext _context;
    private readonly IWebHostEnvironment _env;

    public ReviewsController(ShopContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet("product/{productId}")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetByProduct(int productId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.ProductId == productId)
            .Include(r => r.User)
                .ThenInclude(u => u.Images)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var reviewIds = reviews.Select(r => r.Id).ToList();
        var reviewImages = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Review && reviewIds.Contains(i.OwnerId))
            .ToListAsync();

        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            Rating = r.Rating,
            Title = r.Title,
            Body = r.Body,
            CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            UserName = r.User.Name,
            UserAvatar = r.User.Images
                .Where(i => i.OwnerType == OwnerType.User && i.Position == 0)
                .Select(i => i.Url)
                .FirstOrDefault(),
            Images = reviewImages
                .Where(i => i.OwnerId == r.Id)
                .OrderBy(i => i.Position)
                .Select(i => i.Url)
                .ToList(),
            UserEmail = r.User.Email
        }).ToList();
    }

    [Authorize]
    [HttpPost("product/{productId}")]
    public async Task<IActionResult> Create(
        int productId,
        [FromForm] CreateReviewRequest request)
    {
        var email = User.Identity!.Name;
        var user = await _context.Users.FirstAsync(u => u.Email == email);

        if (await _context.Reviews.AnyAsync(r =>
            r.ProductId == productId && r.UserId == user.Id))
        {
            return BadRequest("You already reviewed this product.");
        }

        var review = new Review
        {
            ProductId = productId,
            UserId = user.Id,
            Rating = request.Rating,
            Title = request.Title,
            Body = request.Body,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        if (request.Images != null && request.Images.Count > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            int position = 0;
            foreach (var file in request.Images)
            {
                if (file == null || file.Length == 0)
                    continue;

                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                _context.Images.Add(new Image
                {
                    OwnerId = review.Id,
                    OwnerType = OwnerType.Review,
                    Url = $"/uploads/{fileName}",
                    Position = position++
                });
            }

            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] CreateReviewRequest request)
    {
        var email = User.Identity!.Name;

        var review = await _context.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null) return NotFound();
        if (review.User.Email != email) return Forbid();

        review.Rating = request.Rating;
        review.Title = request.Title;
        review.Body = request.Body;

       if (request.RemovedImages != null && request.RemovedImages.Count > 0)
        {
            var removedRelativeUrls = request.RemovedImages
                .Select(url => new Uri(url).AbsolutePath)
                .ToList();

            var imagesToRemove = _context.Images
                .Where(i => i.OwnerType == OwnerType.Review 
                            && i.OwnerId == review.Id
                            && removedRelativeUrls.Contains(i.Url));

            foreach (var img in await imagesToRemove.ToListAsync())
            {
                var filePath = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.Images.RemoveRange(await imagesToRemove.ToListAsync());
        }



        if (request.Images != null && request.Images.Count > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            int position = await _context.Images
                .Where(i => i.OwnerType == OwnerType.Review && i.OwnerId == review.Id)
                .Select(i => (int?)i.Position)
                .MaxAsync() ?? 0;

            foreach (var file in request.Images)
            {
                if (file == null || file.Length == 0) continue;

                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                _context.Images.Add(new Image
                {
                    OwnerId = review.Id,
                    OwnerType = OwnerType.Review,
                    Url = $"/uploads/{fileName}",
                    Position = ++position
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }


    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var email = User.Identity!.Name;

        var review = await _context.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null) return NotFound();
        if (review.User.Email != email) return Forbid();

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetMyReviews()
    {
        var email = User.Identity!.Name;

        var reviews = await _context.Reviews
            .Include(r => r.Product)
            .Where(r => r.User.Email == email)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var reviewIds = reviews.Select(r => r.Id).ToList();
        var reviewImages = await _context.Images
            .Where(i => i.OwnerType == OwnerType.Review && reviewIds.Contains(i.OwnerId))
            .ToListAsync();

        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            Rating = r.Rating,
            Title = r.Title,
            Body = r.Body,
            CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            UserName = r.User.Name,
            UserAvatar = r.User.Images
                .Where(i => i.OwnerType == OwnerType.User && i.Position == 0)
                .Select(i => i.Url)
                .FirstOrDefault(),
            Images = reviewImages
                .Where(i => i.OwnerId == r.Id)
                .OrderBy(i => i.Position)
                .Select(i => i.Url)
                .ToList(),
            UserEmail = r.User.Email
        }).ToList();
    }
}

