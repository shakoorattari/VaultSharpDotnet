using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace VaultSharpDotnet.VaultSharp
{
    public class VaultConfigurationProvider : ConfigurationProvider
    {
        public VaultOptions _config;
        private IVaultClient _client;

        public VaultConfigurationProvider(VaultOptions config)
        {
            _config = config;

            var vaultClientSettings = new VaultClientSettings(
                _config.Address,
                new TokenAuthMethodInfo(_config.Secret)
            );

            _client = new VaultClient(vaultClientSettings);
        }

        public override void Load()
        {
            LoadAsync().Wait();
        }

        public async Task LoadAsync()
        {
            await GetDatabaseCredentials();
        }

        public async Task GetDatabaseCredentials()
        {
            try
            {
                // allow mount path override via configuration (defaults to "secret")
                var mount = string.IsNullOrWhiteSpace(_config.MountPath) ? "secret" : _config.MountPath;

                var kv2Secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync("invoice", null, mount);

                if (kv2Secret?.Data?.Data != null && kv2Secret.Data.Data.Any())
                {
                    foreach (var kv in kv2Secret.Data.Data)
                    {
                        // overwrite or add the value into configuration data
                        Data[kv.Key] = kv.Value?.ToString();
                    }
                }
                else
                {
                    // No secret found at the expected path — log and continue so app can start.
                    Console.WriteLine($"[VaultConfigurationProvider] no data found at '{mount}/invoice' — continuing without Vault secrets.");
                }
            }
            catch (Exception ex)
            {
                // Don't fail application startup for Vault read errors in development/demo scenarios.
                // In production you should surface or handle this according to your availability requirements.
                Console.WriteLine($"[VaultConfigurationProvider] failed to read secrets from Vault: {ex.Message}");
            }
        }
    }
}
