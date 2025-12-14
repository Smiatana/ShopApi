using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ScraperController : ControllerBase
{
    private readonly ScraperService _scraper;
    private readonly ShopContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpFactory;

    public ScraperController(
        ScraperService scraper,
        ShopContext context,
        IWebHostEnvironment env,
        IHttpClientFactory httpFactory)
    {
        _scraper = scraper;
        _context = context;
        _env = env;
        _httpFactory = httpFactory;
    }

    [HttpPost("parse")]
    public async Task<IActionResult> Parse([FromBody] ParseRequest req)
    {
        var category = await _context.Categories.FindAsync(req.CategoryId);
        if (category == null)
            return BadRequest("Category not found");

        var results = new List<object>();

        for (int p = req.PageFrom; p <= req.PageTo; p++)
        {
            var html = await _httpFactory.CreateClient()
                .GetStringAsync(BuildPageUrl(req.Url, p));

            var links = await _scraper.ExtractProductLinksWithPriceFromListPage(html);

            foreach (var link in links)
            {
                var scraped = await _scraper.ScrapeProductFromUrl(
                    link.Url,
                    link.Name,
                    link.Price,
                    category.Id
                );

                scraped.Product.CategoryId = category.Id;

                results.Add(new
                {
                    url = link.Url,
                    product = scraped.Product,
                    images = scraped.RemoteImageUrls,
                    errors = scraped.Errors,

                });
            }
        }

        return Ok(results);
        }
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportRequest req)
    {
        var client = _httpFactory.CreateClient();
        var uploads = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploads);

        var category = await _context.Categories.FindAsync(req.CategoryId);
        if (category == null)
            return BadRequest("Category not found");

        foreach (var scrapedItem in req.Products)
        {
            var scrapedProduct = scrapedItem.Product;

            bool exists = await _context.Products.AnyAsync(p =>
                p.Name == scrapedProduct.Name && p.Brand == scrapedProduct.Brand);

            if (exists) continue;

            var product = new Product
            {
                Name = scrapedProduct.Name,
                Brand = scrapedProduct.Brand,
                Price = scrapedProduct.Price,
                Description = scrapedProduct.Description,
                CategoryId = req.CategoryId,
                StockQuantity = scrapedProduct.StockQuantity,
                Specs = scrapedProduct.Specs ?? new Dictionary<string, object>()
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync(); // now product.Id is assigned

            int pos = 0;
            foreach (var imgUrl in scrapedItem.RemoteImageUrls)
            {
                if (string.IsNullOrWhiteSpace(imgUrl)) continue;

                var bytes = await client.GetByteArrayAsync(imgUrl);
                var fileName = $"{Guid.NewGuid()}.webp";
                var path = Path.Combine(uploads, fileName);
                await System.IO.File.WriteAllBytesAsync(path, bytes);

                var image = new Image
                {
                    Url = $"/uploads/{fileName}",
                    OwnerId = product.Id,
                    OwnerType = OwnerType.Product,
                    Position = pos++,
                    AltText = product.Name
                };

                _context.Images.Add(image);
            }

            await _context.SaveChangesAsync();
        }

        return Ok(new { status = "Imported" });
    }

    private string BuildPageUrl(string baseUrl, int page)
    {
        return baseUrl.Contains("?")
            ? $"{baseUrl}&page={page}"
            : $"{baseUrl}?page={page}";
    }
}

