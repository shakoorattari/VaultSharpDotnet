using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VaultSharpDotnet.VaultSharp;

namespace VaultSharpDotnet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {

                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    // load environment-specific settings so "Vault:Secret" from appsettings.Development.json is available here
                    config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables(prefix: "VAULT_");

                    var builtConfig = config.Build();

                    // Only add the Vault provider when a Vault token is configured (avoids startup failure when token is absent)
                    if (!string.IsNullOrWhiteSpace(builtConfig["Vault:Secret"]))
                    {
                        config.AddVault(options =>
                        {
                            var vaultOptions = builtConfig.GetSection("Vault");
                            options.Address = vaultOptions["Address"];
                            options.Role = vaultOptions["Role"];
                            options.MountPath = vaultOptions["MountPath"];
                            options.SecretType = vaultOptions["SecretType"];
                            options.Secret = vaultOptions["Secret"];
                            options.SecretMount = vaultOptions["SecretMount"];
                            options.SecretPath = vaultOptions["SecretPath"];
                        });
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
