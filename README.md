# ManualMapping.Net

Expression-first object mapper for .NET. Write one C# expression that powers both in-memory `Map()` and EF Core `ProjectTo()` — no reflection, no convention magic.

## Why

- **One expression, two uses** — the same `Expression<Func<TSrc, TDest>>` compiles to a delegate for `Map()` and translates to SQL for `ProjectTo()`
- **Zero reflection at runtime** — typed delegates, no `DynamicInvoke`
- **Full control** — you write every property mapping, you see exactly what SQL gets generated
- **DI-friendly** — inject services into converters, override `Convert()` for custom logic
- **EF Core ProjectTo** — ternaries become `CASE WHEN`, navigation properties become `JOIN`, nested collections become sub-selects

## Quick Start

### 1. Define a converter

```csharp
public class OrderConverter : TypeConverter<Order, OrderDto>
{
    public override Expression<Func<Order, OrderDto>> AsExpression() =>
        src => new OrderDto
        {
            Id               = src.Id,
            CustomerFullName = src.Customer.FirstName + " " + src.Customer.LastName,
            CustomerCity     = src.Customer.Address != null
                                   ? src.Customer.Address.City : "N/A",
            CustomerTier     = src.Customer.IsVip ? "VIP" : "Standard",
            Lines = src.OrderLines.Select(ol => new OrderLineDto
            {
                ProductName  = ol.Product.Name,
                CategoryName = ol.Product.Category != null
                                   ? ol.Product.Category.Name : "Uncategorized",
                PriceRange   = ol.UnitPrice > 50 ? "Premium"
                             : ol.UnitPrice > 10 ? "Mid" : "Budget",
                LineTotal    = ol.Quantity * ol.UnitPrice
            }).ToList()
        };
}
```

### 2. Register and use

```csharp
// Registration
var cfg = new MapperConfiguration();
cfg.CreateMap(new OrderConverter());
IMapper mapper = cfg.Build();

// In-memory mapping
OrderDto dto = mapper.Map<Order, OrderDto>(order);

// Collection mapping
List<OrderDto> dtos = mapper.Map<List<OrderDto>>(orders);

// EF Core ProjectTo — translates to SQL
var dtos = dbContext.Orders
    .ProjectTo<OrderDto>(mapper)
    .Where(d => d.CustomerTier == "VIP")
    .ToListAsync();
```

### 3. DI registration

```csharp
builder.Services.AddSingleton<OrderConverter>();

builder.Services.AddMapper((cfg, sp) =>
{
    cfg.CreateMap<Order, OrderDto, OrderConverter>(sp);
});
```

## Bidirectional Converters

```csharp
public class ProductConverter : BidirectionalConverter<Product, ProductDto>
{
    public override Expression<Func<Product, ProductDto>> AsExpression() =>
        src => new ProductDto { Id = src.Id, DisplayName = src.Name };

    public override Expression<Func<ProductDto, Product>> AsReverseExpression() =>
        src => new Product { Id = src.Id, Name = src.DisplayName };
}

// Both directions registered with a single call
cfg.CreateMap(new ProductConverter());

mapper.Map<Product, ProductDto>(product);    // forward
mapper.Map<ProductDto, Product>(dto);        // reverse
```

## Dependency Injection & Virtual Overrides

All methods on converters are virtual — override them to inject services:

| Method | Default | Override to... |
|---|---|---|
| `AsExpression()` | abstract — must implement | Define the EF-translatable projection |
| `Convert()` | Compiles `AsExpression()` | Use injected services for `Map()` |
| `AsReverseExpression()` | abstract (bidirectional) | Define reverse projection |
| `ConvertBack()` | Compiles `AsReverseExpression()` | Use injected services for reverse `Map()` |

### Expression only (simple)

No DI needed — `Convert()` is auto-compiled from `AsExpression()`:

```csharp
public class SimpleConverter : TypeConverter<Product, ProductDto>
{
    public override Expression<Func<Product, ProductDto>> AsExpression() =>
        src => new ProductDto { Id = src.Id, Name = src.Name };

    // Convert() auto-compiled — no override needed
    // ProjectTo() works
}
```

### DI service + ProjectTo (both paths)

Override `Convert()` for in-memory mapping with services, keep `AsExpression()` for SQL:

```csharp
public class ProductConverter : TypeConverter<Product, ProductDto>
{
    private readonly IPricingService _pricing;

    public ProductConverter(IPricingService pricing) => _pricing = pricing;

    // AsExpression() — EF-translatable, used by ProjectTo()
    public override Expression<Func<Product, ProductDto>> AsExpression() =>
        src => new ProductDto
        {
            Id    = src.Id,
            Name  = src.Name,
            Price = src.Price           // raw price in SQL
        };

    // Convert() — overridden, uses injected service for Map()
    public override ProductDto Convert(Product source) => new()
    {
        Id    = source.Id,
        Name  = source.Name,
        Price = _pricing.ApplyDiscount(source.Price)  // discounted in memory
    };
}
```

```csharp
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<ProductConverter>();

builder.Services.AddMapper((cfg, sp) =>
{
    // withProjectTo: true — both Map() and ProjectTo() work
    cfg.CreateMap<Product, ProductDto, ProductConverter>(sp, withProjectTo: true);
});
```

| Call | What runs | Service used |
|---|---|---|
| `mapper.Map<Product, ProductDto>(p)` | `Convert()` override | yes |
| `db.Products.ProjectTo<ProductDto>(mapper)` | `AsExpression()` → SQL | no |

### DI service only (no ProjectTo)

When the expression can't be translated to SQL (e.g. `string.Join`, complex service logic):

```csharp
cfg.CreateMap<Product, ProductDto, ProductConverter>(sp, withProjectTo: false);

mapper.Map<Product, ProductDto>(product);                    // works — Convert()
dbContext.Products.ProjectTo<ProductDto>(mapper);             // throws InvalidOperationException
```

### Bidirectional with DI

Override both directions:

```csharp
public class UserConverter : BidirectionalConverter<User, UserDto>
{
    private readonly IEncryptionService _crypto;

    public UserConverter(IEncryptionService crypto) => _crypto = crypto;

    public override Expression<Func<User, UserDto>> AsExpression() =>
        src => new UserDto { Id = src.Id, Email = src.Email };

    public override Expression<Func<UserDto, User>> AsReverseExpression() =>
        src => new User { Id = src.Id, Email = src.Email };

    public override UserDto Convert(User source) => new()
    {
        Id    = source.Id,
        Email = _crypto.Decrypt(source.EncryptedEmail)
    };

    public override User ConvertBack(UserDto source) => new()
    {
        Id             = source.Id,
        EncryptedEmail = _crypto.Encrypt(source.Email)
    };
}
```

## Simple vs Complex Mappings

If your expression uses methods EF Core can't translate (e.g. `string.Join`), disable `ProjectTo`:

```csharp
cfg.CreateMap(new ComplexConverter(), withProjectTo: false);

// Map() works — uses compiled expression
mapper.Map<Product, ProductDto>(product);

// ProjectTo() throws with a clear message
dbContext.Products.ProjectTo<ProductDto>(mapper); // InvalidOperationException
```

## Generated SQL Example

The expression with ternaries, null-checks, and nested collections generates:

```sql
SELECT
    "o"."Id",
    "c"."FirstName" || ' ' || "c"."LastName",
    CASE WHEN "a"."Id" IS NOT NULL THEN "a"."City" ELSE 'N/A' END,
    CASE WHEN instr("c"."Email", 'vip') > 0 THEN 'VIP' ELSE 'Standard' END,
    "s"."ProductName",
    CASE WHEN "c0"."Id" IS NOT NULL THEN "c0"."Name" ELSE 'Uncategorized' END,
    CASE
        WHEN ef_compare("o0"."UnitPrice", '50.0') > 0 THEN 'Premium'
        WHEN ef_compare("o0"."UnitPrice", '10.0') > 0 THEN 'Mid'
        ELSE 'Budget'
    END
FROM "Orders" AS "o"
INNER JOIN "Customers" AS "c" ON "o"."CustomerId" = "c"."Id"
LEFT JOIN "Addresses" AS "a" ON "c"."AddressId" = "a"."Id"
LEFT JOIN (
    SELECT ...
    FROM "OrderLines" AS "o0"
    INNER JOIN "Products" AS "p" ON "o0"."ProductId" = "p"."Id"
    LEFT JOIN "Categories" AS "c0" ON "p"."CategoryId" = "c0"."Id"
) AS "s" ON "o"."Id" = "s"."OrderId"
```

## Benchmarks vs AutoMapper

Tested with nested objects (Order -> Customer -> Address, OrderLines -> Product -> Category), ternary conditionals, and nullable navigation properties.

### Map() — in-memory

| Method | Mean | Ratio | Allocated |
|---|---|---|---|
| ManualMapping | 1,148 ns | 1.00x | 1.21 KB |
| AutoMapper | 695 ns | 0.61x | 1.27 KB |

### ProjectTo() — EF Core / SQLite

| Method | Orders | Mean | Ratio |
|---|---|---|---|
| ManualMapping | 50 | 3.26 ms | 1.00x |
| AutoMapper | 50 | 3.27 ms | 1.00x |
| ManualMapping | 500 | 30.3 ms | 1.00x |
| AutoMapper | 500 | 30.6 ms | 1.01x |

**ProjectTo is identical** — the database dominates. AutoMapper is faster for in-memory `Map()` due to optimized typed delegate chains for nested collections.

## Project Structure

```
ManualMapping/
  Abstractions/         IMapper, ITypeConverter, IBidirectionalConverter
  Converters/           TypeConverter<TSrc,TDest>, BidirectionalConverter<TSrc,TDest>
  Configuration/        MapperConfiguration, MapperInstance
  Extensions/           AddMapper(), ProjectTo<TDest>()

ManualMapping.Tests/        xUnit + EF Core SQLite
ManualMapping.Benchmarks/   BenchmarkDotNet vs AutoMapper
```

## License

MIT
