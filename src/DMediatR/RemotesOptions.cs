using Microsoft.Extensions.Options;

namespace DMediatR
{
    [OptionsValidator]
    internal partial class ValidateRemotesOptions : IValidateOptions<RemotesOptions>
    { }

    /// <summary>
    /// Dictionary of HostOptions declaring the configured remotes for that DMediatR node.
    /// </summary>
    public sealed class RemotesOptions : Dictionary<string, HostOptions>
    {
        public const string SectionName = "Remotes";

        [ValidateEnumeratedItems]
        public IEnumerable<HostOptions> ValidatedValues => Values;
    }
}