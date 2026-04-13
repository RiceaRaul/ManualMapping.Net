using System.Linq.Expressions;
using ManualMapping.Converters;
using ManualMapping.Tests.Models;

namespace ManualMapping.Tests.Converters;

public interface IPricingService
{
    decimal ApplyDiscount(decimal price);
}

public class FixedDiscountPricingService : IPricingService
{
    public decimal ApplyDiscount(decimal price) => price * 0.9m; // 10% off
}

/// <summary>
/// Converter that uses an injected service in Convert().
/// AsExpression() provides a basic mapping (without service logic).
/// Convert() is overridden to use the injected IPricingService.
/// Must be registered with withProjectTo: false.
/// </summary>
public class ProductWithPricingConverter : TypeConverter<Product, ProductDto>
{
    private readonly IPricingService _pricing;

    public ProductWithPricingConverter(IPricingService pricing)
        => _pricing = pricing;

    public override Expression<Func<Product, ProductDto>> AsExpression() =>
        src => new ProductDto
        {
            Id          = src.Id,
            DisplayName = src.Name + " [" + src.Category + "]",
            Price       = src.Price
        };

    public override ProductDto Convert(Product source) => new()
    {
        Id          = source.Id,
        DisplayName = source.Name + " [" + source.Category + "]",
        Price       = _pricing.ApplyDiscount(source.Price)
    };
}
