using System.Linq.Expressions;
using ManualMapping.Converters;
using ManualMapping.Tests.Models;

namespace ManualMapping.Tests.Converters;

/// <summary>
/// Simple converter — expression is EF-translatable, so ProjectTo works.
/// </summary>
public class ProductSimpleConverter : BidirectionalConverter<Product, ProductDto>
{
    public override Expression<Func<Product, ProductDto>> AsExpression() =>
        src => new ProductDto
        {
            Id          = src.Id,
            DisplayName = src.Name + " [" + src.Category + "]",
            Price       = src.Price
        };

    public override Expression<Func<ProductDto, Product>> AsReverseExpression() =>
        src => new Product
        {
            Id    = src.Id,
            Name  = src.DisplayName,
            Price = src.Price
        };
}
