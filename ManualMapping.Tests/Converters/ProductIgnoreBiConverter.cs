namespace ManualMapping.Tests.Converters;

using ManualMapping.Converters;
using ManualMapping.Tests.Models;

public class ProductIgnoreBiConverter : AutoBidirectionalConverter<Product, ProductDto>
{
    // Forward  Product → ProductDto : skip Price
    protected override IEnumerable<string> IgnoredProperties()
        => [nameof(Product.Price)];

    // Reverse  ProductDto → Product : skip Id
    protected override IEnumerable<string> IgnoredReverseProperties()
        => [nameof(Product.Id)];
}
