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
                var kv2Secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync("database", null, "secret");
                if (kv2Secret.Data.Data.Any())
                {
                    foreach (var data in kv2Secret.Data.Data)
                    {
                        Data.Add(data.Key, data.Value.ToString());
                    }
                }    
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }
    }
}
