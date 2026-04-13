using ManualMapping.Abstractions;
using ManualMapping.Configuration;
using ManualMapping.Extensions;
using ManualMapping.Tests.Converters;
using ManualMapping.Tests.Data;
using ManualMapping.Tests.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ManualMapping.Tests;

public class MapperTests : IDisposable
{
    private readonly TestDbContext _db;
    private readonly IMapper _simpleMapper;
    private readonly IMapper _complexMapper;
    private readonly IMapper _orderMapper;

    public MapperTests()
    {
        // In-memory SQLite — shared connection keeps it alive
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _db = new TestDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        // Seed product data (used by existing tests)
        _db.Products.AddRange(
            new Product { Id = 1, Name = "Widget",  Category = "Hardware", Price = 9.99m,  Tags = ["tool", "metal"] },
            new Product { Id = 2, Name = "Gadget",  Category = "Electronics", Price = 29.99m, Tags = ["usb", "portable"] },
            new Product { Id = 3, Name = "Gizmo",   Category = "Hardware", Price = 4.50m,  Tags = ["plastic"] }
        );
        _db.SaveChanges();

        // Seed nested domain data (used by order tests)
        var catHardware    = new Category { Id = 1, Name = "Hardware" };
        var catElectronics = new Category { Id = 2, Name = "Electronics" };
        _db.Categories.AddRange(catHardware, catElectronics);

        var address = new Address { Id = 1, Street = "123 Main St", City = "Springfield", Country = "US" };
        _db.Addresses.Add(address);

        var customer = new Customer
        {
            Id = 1, FirstName = "John", LastName = "Doe",
            Email = "john@example.com", AddressId = 1
        };
        _db.Customers.Add(customer);

        // Products with Category navigation (separate from the string-based Category field)
        var prodWidget = new Product { Id = 10, Name = "Bolt",   Category = "", Price = 1.50m, Tags = [], CategoryId = 1 };
        var prodGadget = new Product { Id = 11, Name = "Sensor", Category = "", Price = 15.00m, Tags = [], CategoryId = 2 };
        _db.Products.AddRange(prodWidget, prodGadget);

        var order = new Order { Id = 1, OrderDate = new DateTime(2025, 6, 15), CustomerId = 1 };
        _db.Orders.Add(order);

        _db.OrderLines.AddRange(
            new OrderLine { Id = 1, OrderId = 1, ProductId = 10, Quantity = 5,  UnitPrice = 1.50m },
            new OrderLine { Id = 2, OrderId = 1, ProductId = 11, Quantity = 2, UnitPrice = 15.00m }
        );

        _db.SaveChanges();

        // Simple mapper — with ProjectTo (bidirectional detected automatically)
        var simpleCfg = new MapperConfiguration();
        simpleCfg.CreateMap(new ProductSimpleConverter(), withProjectTo: true);
        _simpleMapper = simpleCfg.Build();

        // Complex mapper — without ProjectTo
        var complexCfg = new MapperConfiguration();
        complexCfg.CreateMap(new ProductComplexConverter(), withProjectTo: false);
        _complexMapper = complexCfg.Build();

        // Order mapper — with ProjectTo for nested navigation
        var orderCfg = new MapperConfiguration();
        orderCfg.CreateMap(new OrderConverter(), withProjectTo: true);
        orderCfg.CreateMap(new OrderLineConverter(), withProjectTo: true);
        _orderMapper = orderCfg.Build();
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    // ── Map() tests ──────────────────────────────────────────

    [Fact]
    public void Map_SimpleConverter_MapsForward()
    {
        var product = new Product { Id = 1, Name = "Test", Category = "Cat", Price = 5m };
        var dto = _simpleMapper.Map<Product, ProductDto>(product);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Test [Cat]", dto.DisplayName);
        Assert.Equal(5m, dto.Price);
    }

    [Fact]
    public void Map_SimpleConverter_MapsReverse()
    {
        var dto = new ProductDto { Id = 2, DisplayName = "Reversed", Price = 10m };
        var product = _simpleMapper.Map<ProductDto, Product>(dto);

        Assert.Equal(2, product.Id);
        Assert.Equal("Reversed", product.Name);
        Assert.Equal(10m, product.Price);
    }

    // ── Collection mapping: Map<IEnumerable<TDest>>(collection) ─

    [Fact]
    public void Map_CollectionAsIEnumerable()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "A", Category = "X", Price = 1m },
            new() { Id = 2, Name = "B", Category = "Y", Price = 2m },
        };

        var dtos = _simpleMapper.Map<IEnumerable<ProductDto>>(products);

        var list = dtos.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("A [X]", list[0].DisplayName);
    }

    [Fact]
    public void Map_CollectionAsList()
    {
        var products = new[]
        {
            new Product { Id = 1, Name = "A", Category = "X", Price = 1m },
            new Product { Id = 2, Name = "B", Category = "Y", Price = 2m },
            new Product { Id = 3, Name = "C", Category = "Z", Price = 3m },
        };

        var dtos = _simpleMapper.Map<List<ProductDto>>(products);

        Assert.IsType<List<ProductDto>>(dtos);
        Assert.Equal(3, dtos.Count);
        Assert.Equal("B [Y]", dtos[1].DisplayName);
    }

    [Fact]
    public void Map_CollectionAsArray()
    {
        var products = new[]
        {
            new Product { Id = 1, Name = "A", Category = "X", Price = 1m },
        };

        var dtos = _simpleMapper.Map<ProductDto[]>(products);

        Assert.IsType<ProductDto[]>(dtos);
        Assert.Single(dtos);
        Assert.Equal("A [X]", dtos[0].DisplayName);
    }

    [Fact]
    public void Map_ComplexConverter_IncludesTagsSummary()
    {
        var product = new Product
        {
            Id = 1, Name = "Widget", Category = "HW", Price = 9.99m,
            Tags = ["a", "b", "c"]
        };
        var dto = _complexMapper.Map<Product, ProductDto>(product);

        Assert.Equal("a, b, c", dto.TagsSummary);
    }

    [Fact]
    public void Map_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _simpleMapper.Map<Product, ProductDto>(null!));
    }

    [Fact]
    public void Map_ThrowsOnUnregistered()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _simpleMapper.Map<ProductDto, string>(new ProductDto()));
    }

    // ── Map<TDest>(object) tests ─────────────────────────────

    [Fact]
    public void MapObject_ResolvesCorrectConverter()
    {
        object product = new Product { Id = 1, Name = "X", Category = "Y", Price = 1m };
        var dto = _simpleMapper.Map<ProductDto>(product);

        Assert.Equal("X [Y]", dto.DisplayName);
    }

    // ── ProjectTo tests (SQLite) ─────────────────────────────

    [Fact]
    public void ProjectTo_SimpleConverter_TranslatesToSql()
    {
        var query = _db.Products
            .ProjectTo<ProductDto>(_simpleMapper)
            .OrderBy(d => d.Id);

        var dtos = query.ToList();

        Assert.Equal(5, dtos.Count);
        Assert.Equal("Widget [Hardware]", dtos[0].DisplayName);
        Assert.Equal(9.99m, dtos[0].Price);
        Assert.Equal("Gadget [Electronics]", dtos[1].DisplayName);
    }

    [Fact]
    public void ProjectTo_GeneratesSelectProjection_NotSelectStar()
    {
        var sql = _db.Products
            .ProjectTo<ProductDto>(_simpleMapper)
            .ToQueryString();

        // Must SELECT individual columns, not SELECT *
        Assert.DoesNotContain("SELECT *", sql, StringComparison.OrdinalIgnoreCase);

        // The projected DTO columns must appear in SQL
        Assert.Contains("\"Id\"", sql);
        Assert.Contains("AS \"DisplayName\"", sql);
        Assert.Contains("\"Price\"", sql);

        // DisplayName is built server-side from Name + Category via concatenation
        Assert.Contains("||", sql);
    }

    [Fact]
    public void ProjectTo_WithFilter_GeneratesWhereInSql()
    {
        var query = _db.Products
            .ProjectTo<ProductDto>(_simpleMapper)
            .Where(d => d.Price > 5m)
            .OrderBy(d => d.Price);

        var sql = query.ToQueryString();
        var dtos = query.ToList();

        // SQL contains WHERE clause
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(3, dtos.Count);
        Assert.Equal("Widget [Hardware]", dtos[0].DisplayName);
        Assert.Equal("Sensor []", dtos[1].DisplayName);
        Assert.Equal("Gadget [Electronics]", dtos[2].DisplayName);
    }

    [Fact]
    public void ProjectTo_ComplexConverter_ThrowsBecauseNotRegistered()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _db.Products.ProjectTo<ProductDto>(_complexMapper));
    }

    // ── DI integration test ──────────────────────────────────

    [Fact]
    public void DI_AddMapper_RegistersAndResolves()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<ProductSimpleConverter>();
        services.AddMapper((cfg, sp) =>
        {
            cfg.CreateMap<Product, ProductDto, ProductSimpleConverter>(sp);
        });

        var provider = services.BuildServiceProvider();
        var mapper = provider.GetService<IMapper>();

        Assert.NotNull(mapper);

        var dto = mapper.Map<Product, ProductDto>(
            new Product { Id = 1, Name = "DI", Category = "Test", Price = 1m });
        Assert.Equal("DI [Test]", dto.DisplayName);
    }

    [Fact]
    public void DI_ConverterWithInjectedService_OverridesConvert()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<IPricingService, FixedDiscountPricingService>();
        services.AddSingleton<ProductWithPricingConverter>();
        services.AddMapper((cfg, sp) =>
        {
            // withProjectTo: false — Convert() uses injected service, not the expression
            cfg.CreateMap<Product, ProductDto, ProductWithPricingConverter>(sp, withProjectTo: false);
        });

        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var dto = mapper.Map<Product, ProductDto>(
            new Product { Id = 1, Name = "Widget", Category = "HW", Price = 100m });

        Assert.Equal("Widget [HW]", dto.DisplayName);
        Assert.Equal(90m, dto.Price); // 10% discount applied by service
    }

    [Fact]
    public void DI_ConverterWithService_MapAndProjectToBothWork()
    {
        // Setup: SQLite in-memory for ProjectTo
        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        using var db = new TestDbContext(dbOptions);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        db.Products.AddRange(
            new Product { Id = 100, Name = "Alpha", Category = "Cat1", Price = 200m, Tags = [] },
            new Product { Id = 101, Name = "Beta",  Category = "Cat2", Price = 50m,  Tags = [] }
        );
        db.SaveChanges();

        // DI: converter with injected service + withProjectTo: true
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<IPricingService, FixedDiscountPricingService>();
        services.AddSingleton<ProductWithPricingConverter>();
        services.AddMapper((cfg, sp) =>
        {
            cfg.CreateMap<Product, ProductDto, ProductWithPricingConverter>(sp, withProjectTo: true);
        });
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        // Map() — uses overridden Convert() with injected pricing service
        var dto = mapper.Map<Product, ProductDto>(
            new Product { Id = 1, Name = "X", Category = "Y", Price = 100m });
        Assert.Equal(90m, dto.Price); // 10% discount from service

        // ProjectTo() — uses AsExpression(), no discount (raw price)
        var projected = db.Products
            .ProjectTo<ProductDto>(mapper)
            .OrderBy(d => d.Id)
            .ToList();

        Assert.Equal(2, projected.Count);
        Assert.Equal("Alpha [Cat1]", projected[0].DisplayName);
        Assert.Equal(200m, projected[0].Price); // raw price — no service in SQL
        Assert.Equal("Beta [Cat2]", projected[1].DisplayName);
        Assert.Equal(50m, projected[1].Price);
    }

    // ── Nested Order Map() tests ─────────────────────────────

    [Fact]
    public void Map_OrderConverter_MapsNestedCustomerAddress()
    {
        var order = new Order
        {
            Id = 1,
            OrderDate = new DateTime(2025, 6, 15),
            CustomerId = 1,
            Customer = new Customer
            {
                Id = 1, FirstName = "Jane", LastName = "Smith",
                Email = "jane@test.com",
                Address = new Address { Id = 1, Street = "456 Oak Ave", City = "Portland", Country = "US" }
            },
            OrderLines =
            [
                new OrderLine
                {
                    Id = 1, OrderId = 1, ProductId = 10, Quantity = 3, UnitPrice = 2.50m,
                    Product = new Product
                    {
                        Id = 10, Name = "Bolt", Category = "", Price = 2.50m, Tags = [],
                        ProductCategory = new Category { Id = 1, Name = "Hardware" }
                    }
                }
            ]
        };

        var dto = _orderMapper.Map<Order, OrderDto>(order);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Jane Smith", dto.CustomerFullName);
        Assert.Equal("Portland", dto.CustomerCity);
        Assert.Single(dto.Lines);
        Assert.Equal("Bolt", dto.Lines[0].ProductName);
        Assert.Equal("Hardware", dto.Lines[0].CategoryName);
        Assert.Equal(7.50m, dto.Lines[0].LineTotal);
    }

    [Fact]
    public void Map_OrderLineConverter_MapsNestedProductCategory()
    {
        var orderLine = new OrderLine
        {
            Id = 1, OrderId = 1, ProductId = 10, Quantity = 4, UnitPrice = 5.00m,
            Product = new Product
            {
                Id = 10, Name = "Sensor", Category = "", Price = 5.00m, Tags = [],
                ProductCategory = new Category { Id = 2, Name = "Electronics" }
            }
        };

        var dto = _orderMapper.Map<OrderLine, OrderLineDto>(orderLine);

        Assert.Equal("Sensor", dto.ProductName);
        Assert.Equal("Electronics", dto.CategoryName);
        Assert.Equal(4, dto.Quantity);
        Assert.Equal(5.00m, dto.UnitPrice);
        Assert.Equal(20.00m, dto.LineTotal);
    }

    // ── Nested Order ProjectTo tests ─────────────────────────

    [Fact]
    public void ProjectTo_OrderConverter_JoinsNavigationProperties()
    {
        var sql = _db.Orders
            .ProjectTo<OrderDto>(_orderMapper)
            .ToQueryString();

        // Should generate JOINs for Customer and Address
        Assert.Contains("JOIN", sql, StringComparison.OrdinalIgnoreCase);

        var dtos = _db.Orders
            .ProjectTo<OrderDto>(_orderMapper)
            .ToList();

        Assert.Single(dtos);
        var dto = dtos[0];
        Assert.Equal("John Doe", dto.CustomerFullName);
        Assert.Equal("Springfield", dto.CustomerCity);
        Assert.Equal(new DateTime(2025, 6, 15), dto.OrderDate);
    }

    [Fact]
    public void ProjectTo_OrderConverter_IncludesNestedOrderLines()
    {
        var dtos = _db.Orders
            .ProjectTo<OrderDto>(_orderMapper)
            .ToList();

        Assert.Single(dtos);
        var dto = dtos[0];

        Assert.Equal(2, dto.Lines.Count);

        var bolt = dto.Lines.Single(l => l.ProductName == "Bolt");
        Assert.Equal("Hardware", bolt.CategoryName);
        Assert.Equal(5, bolt.Quantity);
        Assert.Equal(1.50m, bolt.UnitPrice);
        Assert.Equal(7.50m, bolt.LineTotal);

        var sensor = dto.Lines.Single(l => l.ProductName == "Sensor");
        Assert.Equal("Electronics", sensor.CategoryName);
        Assert.Equal(2, sensor.Quantity);
        Assert.Equal(15.00m, sensor.UnitPrice);
        Assert.Equal(30.00m, sensor.LineTotal);
    }
}
