using Microsoft.EntityFrameworkCore;

namespace ManualMapping.Benchmarks;

public class BenchmarkDbContext : DbContext
{
    public DbSet<Product>        Products   => Set<Product>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<Address>        Addresses  => Set<Address>();
    public DbSet<Customer>       Customers  => Set<Customer>();
    public DbSet<Order>          Orders     => Set<Order>();
    public DbSet<OrderLine>      OrderLines => Set<OrderLine>();

    public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.ProductCategory)
             .WithMany()
             .HasForeignKey(p => p.CategoryId)
             .IsRequired(false);
        });

        modelBuilder.Entity<CategoryEntity>(e => e.HasKey(c => c.Id));
        modelBuilder.Entity<Address>(e => e.HasKey(a => a.Id));

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
