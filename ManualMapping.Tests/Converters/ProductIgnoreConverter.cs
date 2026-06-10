namespace ManualMapping.Tests.Converters;

using ManualMapping.Converters;
using ManualMapping.Tests.Models;

public class ProductIgnoreConverter : AutoTypeConverter<Product, ProductDto>
{
    protected override IEnumerable<string> IgnoredProperties()
        => [nameof(Product.Price)];
}
