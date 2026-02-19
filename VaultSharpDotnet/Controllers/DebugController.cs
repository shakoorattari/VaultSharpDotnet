using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using VaultSharpDotnet.Data;

namespace VaultSharpDotnet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ProductsDbContext _db;

        public DebugController(IConfiguration config, ProductsDbContext db)
        {
            _config = config;
            _db = db;
        }

        [HttpGet("db-status")]
        public IActionResult GetDbStatus()
        {
            var conn = _config["db:connectionString"] ?? "(not configured)";
            var redacted = RedactConnectionString(conn);

            string provider = _db?.Database?.ProviderName ?? "(no db)";
            bool canConnect = false;
            string dbConnString = null;

            try
            {
                canConnect = _db.Database.CanConnect();
                dbConnString = _db.Database.GetDbConnection().ConnectionString;
            }
            catch (Exception ex)
            {
                dbConnString = $"(error retrieving): {ex.Message}";
            }

            return Ok(new
            {
                configuredConnection = redacted,
                provider,
                canConnect,
                activeConnection = RedactConnectionString(dbConnString)
            });
        }

        private static string RedactConnectionString(string cs)
        {
            if (string.IsNullOrEmpty(cs)) return cs;
            // simple redact: hide password
            return cs.Replace("Password=", "Password=****").Replace("Pwd=", "Pwd=****");
        }
    }
}