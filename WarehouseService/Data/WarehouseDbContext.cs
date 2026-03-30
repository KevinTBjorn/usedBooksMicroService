using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WarehouseService.Models;         
using Domain;                          

namespace WarehouseService.Data
{
    public class WarehouseDbContext : DbContext
    {
        public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options)
        {
        }

        public DbSet<WarehouseService.Models.Book> Books { get; set; } = null!;
        public DbSet<UserBook> UserBooks { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map table names
            modelBuilder.Entity<WarehouseService.Models.Book>().ToTable("Books");
            modelBuilder.Entity<UserBook>().ToTable("UserBooks");

            // Primary keys
            modelBuilder.Entity<WarehouseService.Models.Book>()
                .HasKey(b => b.Id);

            modelBuilder.Entity<UserBook>()
                .HasKey(x => new { x.BookId, x.UserId });

            // Relationship
            modelBuilder.Entity<UserBook>()
                .HasOne(x => x.Book)
                .WithMany(b => b.UserBooks)
                .HasForeignKey(x => x.BookId);

            // ENUM conversion (IMPORTANT!)
            var genreConverter = new EnumToStringConverter<GenreEnum.BookGenre>();

            modelBuilder.Entity<WarehouseService.Models.Book>()
                .Property(b => b.Genre)
                .HasConversion(genreConverter)
                .HasColumnType("text");

            base.OnModelCreating(modelBuilder);
        }
    }
}