namespace VaultSharpDotnet.Models
{
    public class InvoiceCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }

        // Masked password helper for display in UIs/logs (never reveal full secret in production)
        public string MaskedPassword => string.IsNullOrEmpty(Password) ? null : new string('*', Password.Length);
    }
}