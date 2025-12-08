namespace Aspire.Hosting
{
    /// <summary>
    /// Maps to the "Ngrok" section in appsettings. This is a simple POCO intended
    /// for application-level configuration binding.
    /// </summary>
    public class NgrokOptions
    {
        private string _domain = string.Empty;
        private string _hostname = string.Empty;
        
        /// <summary>
        /// Resource name for the ngrok resource in the AppHost.
        /// </summary>
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>
        /// The ngrok plan (for example "hobbyist").
        /// </summary>
        public string Plan { get; set; } = string.Empty;

        /// <summary>
        /// Mode (http/tcp etc.).
        /// </summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>
        /// Optional domain or public url configured for ngrok.
        /// Setting this will attempt to extract and populate the <see cref="Hostname"/>.
        /// </summary>
        public string Domain
        {
            get => _domain;
            set
            {
                _domain = value ?? string.Empty;
                var h = Sanitize(_domain);
                if (!string.IsNullOrWhiteSpace(h)) _hostname = h!;
            }
        }

        /// <summary>
        /// The sanitized hostname extracted from <see cref="Domain"/>, or the explicitly set hostname.
        /// </summary>
        public string Hostname
        {
            get => !string.IsNullOrWhiteSpace(_hostname) ? _hostname : (Sanitize(_domain) ?? string.Empty);
            set => _hostname = value ?? string.Empty;
        }

        static string? Sanitize(string? d)
        {
            if (string.IsNullOrWhiteSpace(d)) return null;
            try
            {
                var s = d!.Trim();
                if (!s.Contains("://")) s = "http://" + s;
                if (Uri.TryCreate(s, UriKind.Absolute, out var u)) return u.Host;
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Optional host port the ngrok inspection API is exposed on.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Optional container target port exposed by ngrok.
        /// </summary>
        public int TargetPort { get; set; }

        /// <summary>
        /// Optional container target hostname exposed by ngrok.
        /// </summary>
        public string TargetHostname { get; set; } = string.Empty;
    }
}
