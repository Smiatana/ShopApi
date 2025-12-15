// good luck navigating in this file. im just too lazy to create separate for each dto

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public IFormFile? Image { get; set; }
    public string? AltText { get; set; }
}

public class CreateProductRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string Brand { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> Specs { get; set; }
    public int StockQuantity { get; set; }

    public List<IFormFile>? Images { get; set; }
}

public class UpdateProductRequest
{
    public string Name { get; set; }
    public string Brand { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    public string? Specs { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }

    public List<IFormFile>? NewImages { get; set; }
}

public class UpdateImageOrderRequest
{
    public List<ImageOrderItem> Items { get; set; }
}

public class ImageOrderItem
{
    public int ImageId { get; set; }
    public int Position { get; set; }
}

public class ParseRequest
{
    public string Url { get; set; } = "";
    public int PageFrom { get; set; } = 1;
    public int PageTo { get; set; } = 1;
    public int CategoryId { get; set; }
}

public class ProductLinkInfo
{
    public string Url { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; } = 0;
}

public class ScrapedProductResult
{
    public Product Product { get; set; } = null!;
    public List<string> RemoteImageUrls { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class ImportRequest
{
    public int CategoryId { get; set; }
    public List<ScrapedProductResult> Products { get; set; } = new();
}

public class CreateDiscountRequest
{
    public int ProductId { get; set; }
    public decimal Percentage { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
}

public class UpdateDiscountRequest
{
    public decimal Percentage { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool Active { get; set; }
}

public class DiscountDto
{
    public int Id { get; set; }
    public decimal Percentage { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool Active { get; set; }
    public ProductNameDto Product { get; set; }
}

public class ProductNameDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}
