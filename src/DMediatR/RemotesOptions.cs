using Microsoft.Extensions.Options;

namespace DMediatR
{
    [OptionsValidator]
    internal partial class ValidateRemotesOptions : IValidateOptions<RemotesOptions>
    { }

    // The [OptionsValidator] actually validates [ValidateEnumeratedItems], thus ignore all related warnings:
#pragma warning disable IDE0079
#pragma warning disable SYSLIB1212
#pragma warning disable SYSLIB1213

    /// <summary>
    /// Dictionary of HostOptions declaring the configured remotes for that DMediatR node.
    /// </summary>
    public sealed class RemotesOptions : Dictionary<string, HostOptions>
    {
        public const string SectionName = "DMediatR:Remotes";

        [ValidateEnumeratedItems]
        public IEnumerable<HostOptions> ValidatedValues => Values;
    }

#pragma warning restore SYSLIB1213
#pragma warning restore SYSLIB1212
#pragma warning restore IDE0079
}