using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VaultSharpDotnet.Models;

namespace VaultSharpDotnet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly InvoiceCredentials _credentials;

        public InvoiceController(IConfiguration configuration, IOptions<InvoiceCredentials> creds)
        {
            _configuration = configuration;
            _credentials = creds?.Value ?? new InvoiceCredentials();
        }

        // Demo endpoint: returns the vault-provided username/password (development only!)
        [HttpGet("credentials")]
        public IActionResult GetCredentialsFromConfiguration()
        {
            // NOTE: for production, do NOT expose secrets via public endpoints. Use ACLs and restricted APIs.
            var username = _configuration["username"] ?? _credentials.Username;
            var password = _configuration["password"] ?? _credentials.Password;

            return Ok(new { username, password });
        }

        // Safer demo: returns masked password and explains redaction
        [HttpGet("credentials/redacted")]
        public IActionResult GetRedactedCredentials()
        {
            var username = _configuration["username"] ?? _credentials.Username;
            var password = _configuration["password"] ?? _credentials.Password;

            return Ok(new { username, password = Mask(password) });
        }

        private static string Mask(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (value.Length <= 2) return new string('*', value.Length);
            return new string('*', value.Length - 2) + value.Substring(value.Length - 2);
        }
    }
}