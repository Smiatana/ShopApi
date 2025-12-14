using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

public class ScraperService
{
    private readonly IHttpClientFactory _httpFactory;

    public ScraperService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    private string ExtractBrand(string url, string name)
    {

        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length >= 2)
            {
                var candidate = segments[1];

                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return char.ToUpper(candidate[0]) + candidate[1..];
                }
            }
        }
        catch
        {
            
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var tokens = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.Length > 1 && char.IsUpper(token[0]))
                    return token;
            }
        }

        return "Unknown";
    }


    private List<string> ExtractProductImages(HtmlDocument doc)
    {
        var imageUrls = new List<string>();

        var imgNodes = doc.DocumentNode.SelectNodes(
            "//div[contains(@class,'swiper-wrapper')]//div[contains(@class,'swiper-slide')]//img"
        );

        if (imgNodes != null)
        {
            foreach (var img in imgNodes)
            {
                var src = img.GetAttributeValue("src", null);
                if (!string.IsNullOrWhiteSpace(src))
                {
                    imageUrls.Add(src);
                }
            }
        }

        return imageUrls.Distinct().ToList();
    }



    public async Task<List<ProductLinkInfo>> ExtractProductLinksWithPriceFromListPage(string pageHtml)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(pageHtml);

        var result = new List<ProductLinkInfo>();

        var cards = doc.DocumentNode.SelectNodes(
            "//div[contains(@class,'catalog-form__offers-item_primary')]"
        );

        if (cards == null)
            return result;

        foreach (var card in cards)
        {
            var linkNode = card.SelectSingleNode(".//a[contains(@class,'catalog-form__preview')]");
            var href = linkNode?.GetAttributeValue("href", null);

            if (string.IsNullOrWhiteSpace(href))
                continue;

            var url = href.StartsWith("http")
                ? href
                : "https://catalog.onliner.by" + href;

            var nameNode = card.SelectSingleNode(
                ".//h3[contains(@class,'catalog-form__description')]//a"
            );

            var name = nameNode != null
                ? HtmlEntity.DeEntitize(nameNode.InnerText).Trim()
                : "";

            decimal price = 0;

            var priceNode = card.SelectSingleNode(
                ".//a[contains(@class,'catalog-form__link_huge-additional')]//span[last()]"
            );

            if (priceNode != null)
            {
                var rawPrice = HtmlEntity.DeEntitize(priceNode.InnerText)
                    .Replace("\u00A0", "")
                    .Replace("р.", "")
                    .Replace(",", ".")
                    .Trim();

                decimal.TryParse(
                    rawPrice,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out price
                );
            }

            result.Add(new ProductLinkInfo
            {
                Url = url,
                Name = name,
                Price = price
            });
        }

        return result;
    }


    public async Task<ScrapedProductResult> ScrapeProductFromUrl(
        string url,
        string cardName,
        decimal cardPrice,
        int categoryId
    )
    {
        var client = _httpFactory.CreateClient();
        var html = await client.GetStringAsync(url);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var errors = new List<string>();

        var specs = new Dictionary<string, object>();

        var specRows = doc.DocumentNode.SelectNodes("//tr");
        if (specRows != null)
        {
            foreach (var row in specRows)
            {
                var key = row.SelectSingleNode("./td[1]")?.InnerText?.Trim();
                var val = row.SelectSingleNode("./td[2]")?.InnerText?.Trim();

                if (!string.IsNullOrWhiteSpace(key))
                    specs[key] = val ?? "";
            }
        }

        string description = "";

        var descriptionKey = specs.Keys.FirstOrDefault(k => k.StartsWith("Описание"));
        if (descriptionKey != null)
        {
            description = descriptionKey.Replace("Читать дальше", "").Trim();

            specs.Remove(descriptionKey);
        }

        var imageUrls = ExtractProductImages(doc);

        var product = new Product
        {
            Name = cardName,
            Price = cardPrice,
            Brand = ExtractBrand(url, cardName),
            Description = description,
            CategoryId = categoryId,
            StockQuantity = 0,
            Specs = specs,
            Images = new List<Image>(),
            Reviews = new List<Review>(),
            CartItems = new List<CartItem>(),
            Discounts = new List<Discount>(),
            OrderItems = new List<OrderItem>(),
            Category = null
        };

        return new ScrapedProductResult
        {
            Product = product,
            RemoteImageUrls = imageUrls,
            Errors = errors,
            
        };
    }


}

