namespace DMediatR.Tests.MediatR.LocalRemote
{
    [Remote("RemoteHandler")]
    internal class RemoteHandler : IRequestHandler<Ping, Pong>
    {
        public async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            await Task.FromResult(0);
            return new Pong { Message = request.Message + " remote Pong" };
        }
    }
}