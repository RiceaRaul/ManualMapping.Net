namespace ManualMapping.Benchmarks;

// ── Simple (existing) ─────────────────────────────────────────

public class Product
{
    public int       Id         { get; set; }
    public string    Name       { get; set; } = "";
    public string    Category   { get; set; } = "";
    public decimal   Price      { get; set; }
    public int?      CategoryId { get; set; }
    public CategoryEntity? ProductCategory { get; set; }
}

public class ProductDto
{
    public int    Id          { get; set; }
    public string DisplayName { get; set; } = "";
    public decimal Price      { get; set; }
}

// ── Complex nested (3 levels) ─────────────────────────────────

public class CategoryEntity
{
    public int    Id   { get; set; }
    public string Name { get; set; } = "";
}

public class Address
{
    public int    Id      { get; set; }
    public string Street  { get; set; } = "";
    public string City    { get; set; } = "";
    public string Country { get; set; } = "";
}

public class Customer
{
    public int      Id        { get; set; }
    public string   FirstName { get; set; } = "";
    public string   LastName  { get; set; } = "";
    public string   Email     { get; set; } = "";
    public bool     IsVip     { get; set; }
    public int?     AddressId { get; set; }
    public Address? Address   { get; set; }
}

public class Order
{
    public int             Id         { get; set; }
    public DateTime        OrderDate  { get; set; }
    public int             CustomerId { get; set; }
    public Customer        Customer   { get; set; } = null!;
    public List<OrderLine> OrderLines { get; set; } = [];
}

public class OrderLine
{
    public int     Id        { get; set; }
    public int     OrderId   { get; set; }
    public int     ProductId { get; set; }
    public Product Product   { get; set; } = null!;
    public int     Quantity  { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? Discount { get; set; }
}

// ── DTOs (flattened with conditionals) ────────────────────────

public class OrderDto
{
    public int                Id               { get; set; }
    public DateTime           OrderDate        { get; set; }
    public string             CustomerFullName { get; set; } = "";
    public string             CustomerCity     { get; set; } = "";
    public string             CustomerTier     { get; set; } = "";
    public List<OrderLineDto> Lines            { get; set; } = [];
}

public class OrderLineDto
{
    public int     Id           { get; set; }
    public string  ProductName  { get; set; } = "";
    public string  CategoryName { get; set; } = "";
    public string  PriceRange   { get; set; } = "";
    public int     Quantity     { get; set; }
    public decimal UnitPrice    { get; set; }
    public decimal LineTotal    { get; set; }
}
