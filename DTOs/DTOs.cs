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

public class UserAdminDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Role { get; set; } = "User";
}

public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = null!;
}

public class ChangeRoleRequest
{
    public string NewRole { get; set; } = null!;
}

public class ProductListDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string Brand { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public int StockQuantity { get; set; }
    public List<ImageDto> Images { get; set; } = new();
}

public class ImageDto
{
    public string Url { get; set; }
    public string? AltText { get; set; }
    public int Position { get; set; }
}

public class ProductDetailsDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }

    public string Name { get; set; }
    public string Brand { get; set; }
    public decimal Price { get; set; }

    public string Description { get; set; }
    public Dictionary<string, object> Specs { get; set; } = new();

    public int StockQuantity { get; set; }

    public List<ProductImageDetailsDto> Images { get; set; } = new();
}

public class ProductImageDetailsDto
{
    public int Id { get; set; }
    public string Url { get; set; }
    public string? AltText { get; set; }
    public int Position { get; set; }
}

public class UpdateUserProfileRequest
{
    public string Name { get; set; } = null!;
    public IFormFile? Avatar { get; set; }
}


public class ReviewDto
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public string CreatedAt { get; set; }
    public string UserName { get; set; }
    public string UserAvatar { get; set; }
    public List<string> Images { get; set; }
    public string UserEmail { get; set; }
}

public class CreateReviewRequest
{
    public int Rating { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public List<IFormFile>? Images { get; set; }
    public List<string>? RemovedImages { get; set; }
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string Brand { get; set; }
    public decimal PriceAtPurchase { get; set; }
    public int Quantity { get; set; }
    public ImageDto? FirstImage { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}
public class CreateOrderRequest
{
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class OrderReadDto
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class ComparisonProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public Dictionary<string, object> Specs { get; set; } = new();
}

public class ComparisonSpecRowDto
{
    public string Key { get; set; } = "";
    public List<object?> Values { get; set; } = new();
}

public class ComparisonResponseDto
{
    public int CategoryId { get; set; }
    public List<ComparisonProductDto> Products { get; set; } = new();
    public List<ComparisonSpecRowDto> Specs { get; set; } = new();
}


