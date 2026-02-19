using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VaultSharpDotnet.Data
{
    // Design-time factory used by EF tools to create the DbContext with a known connection string.
    public class ProductsDbContextFactory : IDesignTimeDbContextFactory<ProductsDbContext>
    {
        public ProductsDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ProductsDbContext>();

            // Default to LocalDB (matches README / Vault demo). Tools will use this when running migrations.
            var connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=VaultDemoProducts;Integrated Security=true;TrustServerCertificate=True;";

            optionsBuilder.UseSqlServer(connectionString);

            return new ProductsDbContext(optionsBuilder.Options);
        }
    }
}