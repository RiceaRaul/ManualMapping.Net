using BenchmarkDotNet.Attributes;

namespace ManualMapping.Benchmarks;

[MemoryDiagnoser]
public class MapBenchmarks
{
    private Abstractions.IMapper _manual = null!;
    private global::AutoMapper.IMapper _auto = null!;
    private Order _order = null!;

    [GlobalSetup]
    public void Setup()
    {
        // ManualMapping
        var cfg = new Configuration.MapperConfiguration();
        cfg.CreateMap(new OrderConverter());
        _manual = cfg.Build();

        // AutoMapper — equivalent mapping with conditionals
        var autoConfig = new global::AutoMapper.MapperConfiguration(c =>
        {
            c.CreateMap<OrderLine, OrderLineDto>()
             .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
             .ForMember(d => d.CategoryName, o => o.MapFrom(s =>
                 s.Product.ProductCategory != null ? s.Product.ProductCategory.Name : "Uncategorized"))
             .ForMember(d => d.PriceRange, o => o.MapFrom(s =>
                 s.UnitPrice > 50m ? "Premium" : s.UnitPrice > 10m ? "Mid" : "Budget"))
             .ForMember(d => d.LineTotal, o => o.MapFrom(s =>
                 s.Quantity * (s.Discount != null ? s.UnitPrice - s.Discount.Value : s.UnitPrice)));

            c.CreateMap<Order, OrderDto>()
             .ForMember(d => d.CustomerFullName, o => o.MapFrom(s =>
                 s.Customer.FirstName + " " + s.Customer.LastName))
             .ForMember(d => d.CustomerCity, o => o.MapFrom(s =>
                 s.Customer.Address != null ? s.Customer.Address.City : "N/A"))
             .ForMember(d => d.CustomerTier, o => o.MapFrom(s =>
                 s.Customer.IsVip ? "VIP" : "Standard"))
             .ForMember(d => d.Lines, o => o.MapFrom(s => s.OrderLines));
        });
        _auto = autoConfig.CreateMapper();

        // Build a realistic in-memory order with nested objects
        var categories = new[]
        {
            new CategoryEntity { Id = 1, Name = "Hardware" },
            new CategoryEntity { Id = 2, Name = "Electronics" },
            new CategoryEntity { Id = 3, Name = "Software" },
        };

        var products = Enumerable.Range(1, 10).Select(i => new Product
        {
            Id = i,
            Name = $"Product-{i}",
            Category = categories[i % 3].Name,
            Price = i * 7.5m,
            CategoryId = categories[i % 3].Id,
            ProductCategory = categories[i % 3]
        }).ToArray();

        _order = new Order
        {
            Id = 1,
            OrderDate = new DateTime(2025, 6, 15),
            CustomerId = 1,
            Customer = new Customer
            {
                Id = 1, FirstName = "John", LastName = "Doe",
                Email = "john@test.com", IsVip = true,
                AddressId = 1,
                Address = new Address { Id = 1, Street = "123 Main St", City = "Springfield", Country = "US" }
            },
            OrderLines = Enumerable.Range(1, 10).Select(i => new OrderLine
            {
                Id = i, OrderId = 1, ProductId = products[i - 1].Id,
                Product = products[i - 1],
                Quantity = i * 2,
                UnitPrice = products[i - 1].Price,
                Discount = i % 3 == 0 ? 1.5m : null
            }).ToList()
        };
    }

    [Benchmark(Baseline = true)]
    public OrderDto ManualMapping_NestedOrder()
        => _manual.Map<Order, OrderDto>(_order);

    [Benchmark]
    public OrderDto AutoMapper_NestedOrder()
        => _auto.Map<OrderDto>(_order);
}
