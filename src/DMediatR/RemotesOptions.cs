using Microsoft.Extensions.Options;

namespace DMediatR
{
    [OptionsValidator]
    internal partial class ValidateRemotesOptions : IValidateOptions<RemotesOptions>
    { }

    /// <summary>
    /// Dictionary of configured remotes.
    /// </summary>
    public sealed class RemotesOptions : Dictionary<string, HostOptions>
    {
        public const string SectionName = "Remotes";

        [ValidateEnumeratedItems]
        public IEnumerable<HostOptions> ValidatedValues => Values;
    }
}