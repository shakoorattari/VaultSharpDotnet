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
            // read one or more secret paths and merge values into configuration
            await ReadAndMergeSecret("invoice");
            await ReadAndMergeSecret("database");
        }

        private async Task ReadAndMergeSecret(string secretName)
        {
            try
            {
                var mount = string.IsNullOrWhiteSpace(_config.MountPath) ? "secret" : _config.MountPath;
                var kv2Secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretName, null, mount);

                if (kv2Secret?.Data?.Data != null && kv2Secret.Data.Data.Any())
                {
                    foreach (var kv in kv2Secret.Data.Data)
                    {
                        // prefix database keys to avoid collisions (e.g. db:username)
                        var key = secretName.Equals("database", StringComparison.OrdinalIgnoreCase)
                            ? $"db:{kv.Key}"
                            : kv.Key;

                        Data[key] = kv.Value?.ToString();
                    }
                }
                else
                {
                    Console.WriteLine($"[VaultConfigurationProvider] no data found at '{mount}/{secretName}' — continuing without those secrets.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VaultConfigurationProvider] failed to read '{secretName}' from Vault: {ex.Message}");
            }
        }
    }
}
