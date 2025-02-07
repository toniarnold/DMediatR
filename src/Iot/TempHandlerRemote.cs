namespace Iot
{
    [Remote("CpuTemp")]
    public class TempHandlerRemote : TempHandler, IRemote
    {
        public Remote Remote { get; set; } = default!;

        public TempHandlerRemote(Remote remote)
        {
            Remote = remote;
        }

        public override async Task<double> Handle(TempRequest request, CancellationToken cancellationToken)
        {
            return await this.SendRemote(request, cancellationToken);
        }
    }
}