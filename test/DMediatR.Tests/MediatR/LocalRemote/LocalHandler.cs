namespace DMediatR.Tests.MediatR.LocalRemote
{
    [Local("RemoteHandler")]
    public class LocalHandler : IRequestHandler<Ping, Pong>
    {
        public async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            await Task.FromResult(0);
            return new Pong { Message = request.Message + " local Pong" };
        }
    }
}