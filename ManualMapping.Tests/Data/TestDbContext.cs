using ManualMapping.Tests.Models;
using Microsoft.EntityFrameworkCore;

namespace ManualMapping.Tests.Data;

public class TestDbContext : DbContext
{
    public DbSet<Product>   Products   => Set<Product>();
    public DbSet<Category>  Categories => Set<Category>();
    public DbSet<Address>   Addresses  => Set<Address>();
    public DbSet<Customer>  Customers  => Set<Customer>();
    public DbSet<Order>     Orders     => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            // Store Tags as JSON in SQLite
            e.Property(p => p.Tags)
             .HasConversion(
                 v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                 v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!);

            e.HasOne(p => p.ProductCategory)
             .WithMany()
             .HasForeignKey(p => p.CategoryId)
             .IsRequired(false);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
        });

        modelBuilder.Entity<Address>(e =>
        {
            e.HasKey(a => a.Id);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.Address)
             .WithMany()
             .HasForeignKey(c => c.AddressId)
             .IsRequired(false);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasOne(o => o.Customer)
             .WithMany()
             .HasForeignKey(o => o.CustomerId);
            e.HasMany(o => o.OrderLines)
             .WithOne()
             .HasForeignKey(ol => ol.OrderId);
        });

        modelBuilder.Entity<OrderLine>(e =>
        {
            e.HasKey(ol => ol.Id);
            e.HasOne(ol => ol.Product)
             .WithMany()
             .HasForeignKey(ol => ol.ProductId);
        });
    }
}
