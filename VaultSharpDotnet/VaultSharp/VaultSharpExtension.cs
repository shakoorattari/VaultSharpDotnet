using Microsoft.Extensions.Configuration;
using System;

namespace VaultSharpDotnet.VaultSharp
{
    public static class VaultSharpExtension
    {
        public static IConfigurationBuilder AddVault(this IConfigurationBuilder configuration,
        Action<VaultOptions> options)
        {
            var vaultOptions = new VaultConfigurationSource(options);
            configuration.Add(vaultOptions);
            return configuration;
        }
    }

    public class VaultConfigurationSource : IConfigurationSource
    {
        private VaultOptions _config;

        public VaultConfigurationSource(Action<VaultOptions> config)
        {
            _config = new VaultOptions();
            config.Invoke(_config);
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new VaultConfigurationProvider(_config);
        }
    }
}
