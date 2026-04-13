using AutoMapper;
using AutoMapper.QueryableExtensions;
using BenchmarkDotNet.Attributes;
using ManualMapping.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ManualMapping.Benchmarks;

[MemoryDiagnoser]
public class ProjectToBenchmarks
{
    private BenchmarkDbContext _db = null!;
    private Abstractions.IMapper _manual = null!;
    private IConfigurationProvider _autoConfig = null!;

    [Params(50, 500)]
    public int Orders;

    [GlobalSetup]
    public void Setup()
    {
        // ManualMapping
        var cfg = new Configuration.MapperConfiguration();
        cfg.CreateMap(new OrderConverter());
        _manual = cfg.Build();

        // AutoMapper — same mapping with conditionals
        var autoMapperConfig = new global::AutoMapper.MapperConfiguration(c =>
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
        _autoConfig = autoMapperConfig;

        // SQLite in-memory
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _db = new BenchmarkDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        // Seed categories
        var categories = Enumerable.Range(1, 5).Select(i =>
            new CategoryEntity { Id = i, Name = $"Category-{i}" }).ToList();
        _db.Categories.AddRange(categories);

        // Seed addresses
        var addresses = Enumerable.Range(1, Orders).Select(i =>
            new Address { Id = i, Street = $"Street {i}", City = $"City-{i % 20}", Country = "US" }).ToList();
        _db.Addresses.AddRange(addresses);

        // Seed customers
        var customers = Enumerable.Range(1, Orders).Select(i =>
            new Customer
            {
                Id = i, FirstName = $"First{i}", LastName = $"Last{i}",
                Email = $"user{i}@test.com", IsVip = i % 5 == 0, AddressId = i
            }).ToList();
        _db.Customers.AddRange(customers);

        // Seed products
        var products = Enumerable.Range(1, 20).Select(i =>
            new Product
            {
                Id = i, Name = $"Product-{i}", Category = "",
                Price = i * 5.5m, CategoryId = (i % 5) + 1
            }).ToList();
        _db.Products.AddRange(products);
        _db.SaveChanges();

        // Seed orders with 3-8 lines each
        var orderLineId = 1;
        for (int i = 1; i <= Orders; i++)
        {
            var order = new Order { Id = i, OrderDate = DateTime.Now.AddDays(-i), CustomerId = i };
            _db.Orders.Add(order);

            var lineCount = 3 + (i % 6); // 3-8 lines per order
            for (int j = 0; j < lineCount; j++)
            {
                _db.OrderLines.Add(new OrderLine
                {
                    Id = orderLineId++,
                    OrderId = i,
                    ProductId = (j % 20) + 1,
                    Quantity = j + 1,
                    UnitPrice = ((j % 20) + 1) * 5.5m,
                    Discount = j % 4 == 0 ? 2.0m : null
                });
            }
        }
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    [Benchmark(Baseline = true)]
    public List<OrderDto> ManualMapping_ProjectTo()
        => _db.Orders.ProjectTo<OrderDto>(_manual).ToList();

    [Benchmark]
    public List<OrderDto> AutoMapper_ProjectTo()
        => _db.Orders.ProjectTo<OrderDto>(_autoConfig).ToList();
}
