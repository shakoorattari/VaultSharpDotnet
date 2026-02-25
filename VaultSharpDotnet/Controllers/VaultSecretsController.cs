using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Token;

namespace VaultSharpDotnet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VaultSecretsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public VaultSecretsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Connects directly to Vault using the configured credentials and returns all
        /// key/value pairs found at the configured SecretMount/SecretPath.
        /// Intended for development/testing only — do NOT expose in production.
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestSecrets()
        {
            var address     = _configuration["Vault:Address"];
            var secret      = _configuration["Vault:Secret"];
            var role        = _configuration["Vault:Role"];
            var mountPath   = _configuration["Vault:MountPath"];
            var secretType  = _configuration["Vault:SecretType"];
            var secretMount = _configuration["Vault:SecretMount"] ?? "secret";
            var secretPath  = _configuration["Vault:SecretPath"]  ?? "dev";

            if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(secret))
            {
                return BadRequest(new { error = "Vault:Address or Vault:Secret is not configured." });
            }

            try
            {
                IAuthMethodInfo authMethod;

                if (string.Equals(secretType, "AppRole", StringComparison.OrdinalIgnoreCase))
                {
                    var authMount = string.IsNullOrWhiteSpace(mountPath)
                        ? "approle"
                        : mountPath.Trim('/');

                    authMethod = new AppRoleAuthMethodInfo(authMount, role, secret);
                }
                else
                {
                    authMethod = new TokenAuthMethodInfo(secret);
                }

                var settings = new VaultClientSettings(address, authMethod)
                {
                    // Allow self-signed / internal-CA certificates and persist cookies
                    // for load-balancer session affinity (mirrors the curl behaviour).
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
                var client = new VaultClient(settings);

                var result = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretPath, null, secretMount);

                if (result?.Data?.Data == null || result.Data.Data.Count == 0)
                {
                    return Ok(new
                    {
                        path    = $"{secretMount}/data/{secretPath}",
                        message = "Path reached successfully but no keys were found.",
                        data    = new { }
                    });
                }

                return Ok(new
                {
                    path    = $"{secretMount}/data/{secretPath}",
                    version = result.Data.Metadata?.Version,
                    data    = result.Data.Data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error   = "Failed to retrieve secrets from Vault.",
                    detail  = ex.Message,
                    path    = $"{secretMount}/data/{secretPath}"
                });
            }
        }
    }
}
