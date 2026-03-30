using OrderService.Models;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<UserBook> UserBooks => Set<UserBook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.Property(o => o.UserId).IsRequired();
            entity.Property(o => o.CustomerId).IsRequired();
            entity.Property(o => o.CreatedAt).IsRequired();
            entity.Property(o => o.CorrelationId).IsRequired();
            entity.Property(o => o.Status).IsRequired();

            entity.HasMany(o => o.Items)
                  .WithOne()
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserBook>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.BookId).IsRequired();
            entity.Property(i => i.Quantity).IsRequired();
            entity.Property(i => i.Condition).IsRequired();
            entity.Property(i => i.Price).IsRequired();
        });
    }
}

