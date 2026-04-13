using System.Linq.Expressions;
using ManualMapping.Converters;
using ManualMapping.Tests.Models;

namespace ManualMapping.Tests.Converters;

/// <summary>
/// Maps Order → OrderDto with nested navigation properties (3 levels deep).
/// Expression is EF-translatable for ProjectTo, including nested collection via Select().
/// </summary>
public class OrderConverter : TypeConverter<Order, OrderDto>
{
    public override Expression<Func<Order, OrderDto>> AsExpression() =>
        src => new OrderDto
        {
            Id               = src.Id,
            OrderDate        = src.OrderDate,
            CustomerFullName = src.Customer.FirstName + " " + src.Customer.LastName,
            CustomerCity     = src.Customer.Address!.City,
            Lines = src.OrderLines.Select(ol => new OrderLineDto
            {
                Id           = ol.Id,
                ProductName  = ol.Product.Name,
                CategoryName = ol.Product.ProductCategory!.Name,
                Quantity     = ol.Quantity,
                UnitPrice    = ol.UnitPrice,
                LineTotal    = ol.Quantity * ol.UnitPrice
            }).ToList()
        };
}
