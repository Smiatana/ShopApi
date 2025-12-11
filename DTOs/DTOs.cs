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
    public Dictionary<string, object> Specs { get; set; }
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
