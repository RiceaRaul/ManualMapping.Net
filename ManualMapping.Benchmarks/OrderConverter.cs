using System.Linq.Expressions;
using ManualMapping.Converters;

namespace ManualMapping.Benchmarks;

/// <summary>
/// 3-level deep mapping with conditionals:
/// Order → Customer → Address (City)
/// Order → OrderLines → Product → Category
/// Includes ternary expressions (CustomerTier, PriceRange, Discount)
/// </summary>
public class OrderConverter : TypeConverter<Order, OrderDto>
{
    public override Expression<Func<Order, OrderDto>> AsExpression() =>
        src => new OrderDto
        {
            Id               = src.Id,
            OrderDate        = src.OrderDate,
            CustomerFullName = src.Customer.FirstName + " " + src.Customer.LastName,
            CustomerCity     = src.Customer.Address != null ? src.Customer.Address.City : "N/A",
            CustomerTier     = src.Customer.IsVip ? "VIP" : "Standard",
            Lines = src.OrderLines.Select(ol => new OrderLineDto
            {
                Id           = ol.Id,
                ProductName  = ol.Product.Name,
                CategoryName = ol.Product.ProductCategory != null
                                   ? ol.Product.ProductCategory.Name
                                   : "Uncategorized",
                PriceRange   = ol.UnitPrice > 50m ? "Premium"
                             : ol.UnitPrice > 10m ? "Mid"
                             : "Budget",
                Quantity     = ol.Quantity,
                UnitPrice    = ol.UnitPrice,
                LineTotal    = ol.Quantity * (ol.Discount != null
                                   ? ol.UnitPrice - ol.Discount.Value
                                   : ol.UnitPrice)
            }).ToList()
        };
}
