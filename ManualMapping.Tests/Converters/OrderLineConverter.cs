using System.Linq.Expressions;
using ManualMapping.Converters;
using ManualMapping.Tests.Models;

namespace ManualMapping.Tests.Converters;

/// <summary>
/// Maps OrderLine → OrderLineDto with 3-level navigation (OrderLine → Product → Category).
/// Expression is EF-translatable for ProjectTo.
/// </summary>
public class OrderLineConverter : TypeConverter<OrderLine, OrderLineDto>
{
    public override Expression<Func<OrderLine, OrderLineDto>> AsExpression() =>
        src => new OrderLineDto
        {
            Id           = src.Id,
            ProductName  = src.Product.Name,
            CategoryName = src.Product.ProductCategory!.Name,
            Quantity     = src.Quantity,
            UnitPrice    = src.UnitPrice,
            LineTotal    = src.Quantity * src.UnitPrice
        };
}
