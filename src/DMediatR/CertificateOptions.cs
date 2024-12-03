using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace DMediatR
{
    [OptionsValidator]
    internal partial class ValidateCertificateOptions : IValidateOptions<CertificateOptions>
    { }

    /// <summary>
    /// X509 certificate specification, including the password.
    /// </summary>
    public sealed class CertificateOptions
    {
        public const string SectionName = "Certificate";

        [Required]
        public string? HostName { get; set; }

        [Required]
        public string? FilenamePrefix { get; set; }

        [Required]
        public String? FilePath { get; set; }

        [Required]
        public String? Password { get; set; }

        [Required]
        [Range(typeof(int), "1", "3650")]
        public int? ValidDays { get; set; }

        [Required]
        [Range(typeof(int), "0", "3650")]
        public int? RenewBeforeExpirationDays { get; set; }
    }
}