using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VaultSharpDotnet.Models;

namespace VaultSharpDotnet
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();

            // Bind secrets loaded into IConfiguration by the Vault provider to a strongly-typed options class.
            // This is the recommended pattern for consuming secrets in DI-friendly, testable code.
            services.Configure<InvoiceCredentials>(opts =>
            {
                opts.Username = Configuration["username"];
                opts.Password = Configuration["password"];
            });

            // Configure EF Core DbContext using database credentials retrieved from Vault (keys are prefixed with `db:` by the provider).
            var dbServer = Configuration["db:server"];            
            var dbName = Configuration["db:database"] ?? "VaultDemoProducts";
            var dbUser = Configuration["db:username"];
            var dbPass = Configuration["db:password"];
            var dbConnFromConfig = Configuration["db:connectionString"];

            // Development-friendly default: prefer a local SQLite file for demo persistence so contributors don't need LocalDB.
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var preferSqliteInDev = envName.Equals("Development", StringComparison.OrdinalIgnoreCase);

            if (preferSqliteInDev)
            {
                var dbPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "products.db");
                Console.WriteLine($"[Startup] Development environment detected — using SQLite at '{dbPath}' for demo persistence");
                services.AddDbContext<VaultSharpDotnet.Data.ProductsDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));
            }
            else if (!string.IsNullOrWhiteSpace(dbConnFromConfig))
            {
                // If a full connection string is provided via Vault (production), use SQL Server at runtime.
                Console.WriteLine("[Startup] using db:connectionString from configuration (SQL Server will be used)");
                services.AddDbContext<VaultSharpDotnet.Data.ProductsDbContext>(options =>
                    options.UseSqlServer(dbConnFromConfig));
            }
            else if (!string.IsNullOrWhiteSpace(dbServer))
            {
                // build connection string from Vault-provided values (may be integrated security)
                string connectionString;
                if (!string.IsNullOrWhiteSpace(dbUser) && !string.IsNullOrWhiteSpace(dbPass))
                {
                    connectionString = $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPass};TrustServerCertificate=True;";
                }
                else
                {
                    connectionString = $"Server={dbServer};Database={dbName};Trusted_Connection=True;TrustServerCertificate=True;";
                }

                Console.WriteLine($"[Startup] using built connection string -> DataSource: {dbServer}, Database: {dbName}");
                services.AddDbContext<VaultSharpDotnet.Data.ProductsDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }
            else
            {
                // Fallback to file-based SQLite DB for demo/testing when no DB config present.
                var dbPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "products.db");
                Console.WriteLine($"[Startup] no DB configuration found — using SQLite at '{dbPath}'");
                services.AddDbContext<VaultSharpDotnet.Data.ProductsDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));
            }

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "VaultSharpDotnet", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "VaultSharpDotnet v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Ensure database is created for the demo (safe to call repeatedly).
            try
            {
                using var scope = app.ApplicationServices.CreateScope();
                var db = scope.ServiceProvider.GetService<VaultSharpDotnet.Data.ProductsDbContext>();
                db?.Database.EnsureCreated();
            }
            catch (System.Exception ex)
            {
                // Log and continue — in dev we'll use in-memory fallback if SQL Server is not reachable.
                System.Console.WriteLine($"[Startup] failed to ensure database: {ex.Message}");
            }
        }
    }
}
