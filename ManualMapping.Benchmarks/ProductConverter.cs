using System.Linq.Expressions;
using ManualMapping.Converters;

namespace ManualMapping.Benchmarks;

public class ProductConverter : TypeConverter<Product, ProductDto>
{
    public override Expression<Func<Product, ProductDto>> AsExpression() =>
        src => new ProductDto
        {
            Id          = src.Id,
            DisplayName = src.Name + " [" + src.Category + "]",
            Price       = src.Price
        };
}
