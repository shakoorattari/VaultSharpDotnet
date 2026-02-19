using Microsoft.EntityFrameworkCore;
using VaultSharpDotnet.Models;

namespace VaultSharpDotnet.Data
{
    public class ProductsDbContext : DbContext
    {
        public ProductsDbContext(DbContextOptions<ProductsDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(b =>
            {
                b.HasKey(p => p.Id);
                b.Property(p => p.Name).IsRequired().HasMaxLength(200);
                b.Property(p => p.Price).HasColumnType("decimal(18,2)");
            });
        }
    }
}