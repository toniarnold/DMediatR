using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace DMediatR
{
    [OptionsValidator]
    internal partial class ValidatePasswordOptions : IValidateOptions<PasswordOptions>
    { }

    /// <summary>
    /// X509 certificate password.
    /// </summary>
    public class PasswordOptions
    {
        public const string SectionName = "Certificate";

        [Required]
        public String? Password { get; set; }
    }
}