using System.Linq.Expressions;
using ManualMapping.Converters;
using ManualMapping.Tests.Models;

namespace ManualMapping.Tests.Converters;

/// <summary>
/// Auto-mapping converter: Id and Price auto-map by name+type via reflection.
/// DisplayName needs custom composition; reverse Name needs custom extraction.
/// TagsSummary is left unbound (no matching source property).
/// </summary>
public class ProductAutoConverter : AutoBidirectionalConverter<Product, ProductDto>
{
    protected override Expression<Func<Product, ProductDto>> CustomExpression() =>
        src => new ProductDto
        {
            DisplayName = src.Name + " [" + src.Category + "]"
        };

    protected override Expression<Func<ProductDto, Product>> CustomReverseExpression() =>
        src => new Product
        {
            Name = src.DisplayName
        };
}
