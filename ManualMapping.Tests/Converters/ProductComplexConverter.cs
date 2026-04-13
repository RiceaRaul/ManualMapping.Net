using System.Linq.Expressions;
using ManualMapping.Converters;
using ManualMapping.Tests.Models;

namespace ManualMapping.Tests.Converters;

/// <summary>
/// Complex converter — uses string.Join which EF Core can't translate to SQL.
/// Registered with withProjectTo: false, so only Map() works.
/// </summary>
public class ProductComplexConverter : TypeConverter<Product, ProductDto>
{
    public override Expression<Func<Product, ProductDto>> AsExpression() =>
        src => new ProductDto
        {
            Id = src.Id,
            DisplayName = src.Name + " [" + src.Category + "]",
            Price = src.Price,
            TagsSummary = string.Join(", ", src.Tags)
        };
}
