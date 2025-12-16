public static class ProductMapper
{
    public static ProductListDto ToListDto(
        Product p,
        IEnumerable<Image> images,
        DateTime now)
    {
        var activeDiscount = p.Discounts?
            .Where(d => d.Active && d.ValidFrom <= now && d.ValidTo >= now)
            .OrderByDescending(d => d.Percentage)
            .FirstOrDefault();

        var discountPercentage = activeDiscount?.Percentage ?? 0m;

        return new ProductListDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            Name = p.Name,
            Brand = p.Brand,
            Price = p.Price,
            DiscountPercentage = discountPercentage,
            DiscountedPrice = p.Price * (1 - discountPercentage / 100),
            StockQuantity = p.StockQuantity,
            Images = images
                .Where(i => i.OwnerId == p.Id && i.OwnerType == OwnerType.Product)
                .OrderBy(i => i.Position)
                .Select(i => new ImageDto
                {
                    Url = i.Url,
                    AltText = i.AltText,
                    Position = i.Position
                })
                .ToList()
        };
    }

    public static ProductDetailsDto ToDetailsDto(
        Product p,
        List<Image> images)
    {
        var productImages = images
            .Where(i => i.OwnerType == OwnerType.Product && i.OwnerId == p.Id)
            .OrderBy(i => i.Position)
            .Select(i => new ProductImageDetailsDto
            {
                Id = i.Id,
                Url = i.Url,
                AltText = i.AltText,
                Position = i.Position
            })
            .ToList();

        return new ProductDetailsDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            Name = p.Name,
            Brand = p.Brand,
            Price = p.Price,
            Description = p.Description,
            Specs = p.Specs,
            StockQuantity = p.StockQuantity,
            Images = productImages
        };
    }

}
