namespace VaultSharpDotnet.VaultSharp
{
    public class VaultOptions
    {
        public string Address { get; set; }
        public string Role { get; set; }
        public string Secret { get; set; }
        public string MountPath { get; set; }
        public string SecretType { get; set; }

        /// <summary>KV v2 engine mount name (e.g. "kv-oneportal")</summary>
        public string SecretMount { get; set; }

        /// <summary>Path within the KV engine to read (e.g. "dev")</summary>
        public string SecretPath { get; set; }
    }
}
