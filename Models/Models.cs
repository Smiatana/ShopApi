using System.Text.Json.Serialization;

public enum OwnerType { Product, Category, Review, User }
public enum OrderStatus { Pending, Paid, Shipped, Delivered }

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public Dictionary<string, object> ProfileInfo { get; set; }

    public ICollection<Cart> Carts { get; set; }
    public ICollection<Order> Orders { get; set; }
    public ICollection<Review> Reviews { get; set; }
    public ICollection<Image> Images { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string Brand { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> Specs { get; set; }
    public int StockQuantity { get; set; }

    public Category? Category { get; set; }
    public ICollection<Image>? Images { get; set; }
    public ICollection<Review>? Reviews { get; set; }
    public ICollection<Discount>? Discounts { get; set; }
    public ICollection<OrderItem>? OrderItems { get; set; }
    public ICollection<CartItem>? CartItems { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public ICollection<Product> Products { get; set; }
}

public class Image
{
    public int Id { get; set; }
    
    public int OwnerId { get; set; }
    public string Url { get; set; }
    public string? AltText { get; set; }
    public int Position { get; set; }
    public OwnerType OwnerType { get; set; }

    [JsonIgnore]
    public Category? Category { get; set; }
    [JsonIgnore]
    public Product? Product { get; set; }
    [JsonIgnore]
    public Review? Review { get; set; }
    [JsonIgnore]
    public User? User { get; set; }
}

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public User User { get; set; }
    public ICollection<CartItem> Items { get; set; }
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    public Cart Cart { get; set; }
    public Product Product { get; set; }
}

public class Comparison
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; }
    public int[] Products { get; set; } = []; // [id, id]

    public User User { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; }
    public ICollection<OrderItem> Items { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }

    public Order Order { get; set; }
    public Product Product { get; set; }
}

public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; }
    public Product Product { get; set; }
    public ICollection<Image> Images { get; set; }
}

public class Discount
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal Percentage { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool Active { get; set; }

    public Product Product { get; set; }
}
