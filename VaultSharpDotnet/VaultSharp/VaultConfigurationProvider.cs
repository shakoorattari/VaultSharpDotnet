using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
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

            IAuthMethodInfo authMethod;

            if (string.Equals(_config.SecretType, "AppRole", StringComparison.OrdinalIgnoreCase))
            {
                // MountPath is the approle auth mount (default: "approle")
                var authMount = string.IsNullOrWhiteSpace(_config.MountPath)
                    ? "approle"
                    : _config.MountPath.Trim('/');

                authMethod = new AppRoleAuthMethodInfo(authMount, _config.Role, _config.Secret);
            }
            else
            {
                authMethod = new TokenAuthMethodInfo(_config.Secret);
            }

            var vaultClientSettings = new VaultClientSettings(_config.Address, authMethod)
            {
                // Allow self-signed / internal-CA certificates (common in enterprise Vault deployments).
                // Also enables cookie persistence for load-balancer session affinity.
                PostProcessHttpClientHandlerAction = handler =>
                {
                    if (handler is HttpClientHandler h)
                    {
                        h.UseCookies = true;
                        h.ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }
                }
            };
            _client = new VaultClient(vaultClientSettings);
        }

        public override void Load()
        {
            LoadAsync().Wait();
        }

        public async Task LoadAsync()
        {
            var secretMount = string.IsNullOrWhiteSpace(_config.SecretMount) ? "secret" : _config.SecretMount;
            var secretPath  = string.IsNullOrWhiteSpace(_config.SecretPath)  ? "dev"    : _config.SecretPath;

            await ReadAndMergeSecret(secretPath, secretMount);
        }

        private async Task ReadAndMergeSecret(string secretName, string mount)
        {
            try
            {
                var kv2Secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretName, null, mount);

                if (kv2Secret?.Data?.Data != null && kv2Secret.Data.Data.Any())
                {
                    foreach (var kv in kv2Secret.Data.Data)
                    {
                        Data[kv.Key] = kv.Value?.ToString();
                    }

                    Console.WriteLine($"[VaultConfigurationProvider] loaded {kv2Secret.Data.Data.Count} key(s) from '{mount}/{secretName}'.");
                }
                else
                {
                    Console.WriteLine($"[VaultConfigurationProvider] no data found at '{mount}/{secretName}' — continuing without those secrets.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VaultConfigurationProvider] failed to read '{mount}/{secretName}' from Vault: {ex.Message}");
            }
        }
    }
}
