using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace DMediatR
{
    [OptionsValidator]
    internal partial class ValidateHostOptions : IValidateOptions<HostOptions>
    { }

    /// <summary>
    /// Specification of a DMediatR node host. Also used for specifying the remotes in RemotesOptions.
    /// </summary>
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
        /// Programmatically set during WebApplication configuration, not bound from config.
        /// </summary>
        internal GrpcPort GrpcPort { get; set; } = GrpcPort.UseDefault;

        /// <summary>
        /// HTTPS gRPC address for the default port.
        /// </summary>
        internal string Address => $"https://{Host}:{Port}/";

        /// <summary>
        /// HTTPS gRPC address for the renewal port.
        /// </summary>
        internal string OldAddress => $"https://{Host}:{OldPort}/";
    }
}