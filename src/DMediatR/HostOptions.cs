using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace DMediatR
{
    [OptionsValidator]
    internal partial class ValidateHostOptions : IValidateOptions<HostOptions>
    { }

    public sealed class HostOptions
    {
        public const string SectionName = "Host";

        [Required]
        public String? Host { get; set; }

        [Required]
        [Range(typeof(int), "1", "65535")]
        public String? Port { get; set; }

        [Required]
        [Range(typeof(int), "1", "65535")]
        public String? OldPort { get; set; }

        /// <summary>
        /// Programmatically set during WebApplication configuration, not bound from config
        /// </summary>
        public GrpcPort GrpcPort { get; set; } = GrpcPort.UseDefault;
    }
}