using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace DMediatR
{
    [OptionsValidator]
    internal partial class ValidateCertificateOptions : IValidateOptions<CertificateOptions>
    { }

    /// <summary>
    /// X509 certificate specification, inheriting the password.
    /// </summary>
    public sealed class CertificateOptions : PasswordOptions
    {
        [Required]
        public string? HostName { get; set; }

        [Required]
        public string? FilenamePrefix { get; set; }

        [Required]
        public String? FilePath { get; set; }

        /// <summary>
        /// Default validity period, overridable by optional values for specific certificates.
        /// The upper limit is set to 25 years, but DMediatR encourages short-lived certificates.
        /// </summary>
        [Required]
        [Range(typeof(int), "1", "9125")]
        public int? ValidDays { get; set; }

        [Required]
        [Range(typeof(int), "0", "9125")]
        public int? RenewBeforeExpirationDays { get; set; }

        [Range(typeof(int), "1", "9125")]
        public int? ClientCertificateValidDays { get; set; }

        [Range(typeof(int), "1", "9125")]
        public int? IntermediateCertificateValidDays { get; set; }

        [Range(typeof(int), "1", "9125")]
        public int? RootCertificateValidDays { get; set; }

        [Range(typeof(int), "1", "9125")]
        public int? ServerCertificateValidDays { get; set; }

        /// <summary>
        /// Defaults to True. If the firewall is explicitly disabled, any node
        /// on the network can publish two
        /// RenewIntermediateCertificateNotification messages in a row to bring
        /// down the entire network, which may not be desirable. Chicago-style
        /// functional unit tests are one use case for disabling the firewall.
        /// Another one might be, in a highly trusted scenario, some kind of
        /// intrusion detection on a remote node  that declares the entire
        /// network to be potentially compromised and thus requiring manual
        /// recertification.
        /// </summary>
        public bool RenewFirewallEnabled { get; set; } = true;
    }
}