using Microsoft.EntityFrameworkCore;
using OrdersPipeline.Api.Models;

namespace OrdersPipeline.Api.Data;

internal sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Id).ValueGeneratedOnAdd();
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Category).HasMaxLength(100).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2).IsRequired();
            entity.Property(product => product.Stock).IsRequired();
            entity.Property(product => product.Supplier).HasMaxLength(200).IsRequired();

            entity.HasData(
                new Product
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Laptop Pro 14",
                    Category = "Computers",
                    Price = 1499.99m,
                    Stock = 12,
                    Supplier = "Tech Imports"
                },
                new Product
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Wireless Keyboard",
                    Category = "Accessories",
                    Price = 79.90m,
                    Stock = 35,
                    Supplier = "Office Supply Co."
                },
                new Product
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "USB-C Dock",
                    Category = "Accessories",
                    Price = 189.50m,
                    Stock = 18,
                    Supplier = "Tech Imports"
                },
                new Product
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "27-inch Monitor",
                    Category = "Displays",
                    Price = 329.00m,
                    Stock = 9,
                    Supplier = "Vision Devices"
                },
                new Product
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "Noise-Cancelling Headphones",
                    Category = "Audio",
                    Price = 249.99m,
                    Stock = 22,
                    Supplier = "Sound World"
                });
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Id).ValueGeneratedOnAdd();
            entity.Property(order => order.CreatedAt).IsRequired();
            entity.Property(order => order.UpdatedAt);
            entity.Property(order => order.Status).IsRequired();
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(item => new { item.OrderId, item.ProductId });
            entity.Property(item => item.Quantity).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2).IsRequired();

            entity.HasOne(item => item.Order)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Product)
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
