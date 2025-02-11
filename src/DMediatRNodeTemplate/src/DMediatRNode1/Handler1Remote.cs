namespace DMediatRNode1
{
    [Remote("Handler1")]
    public class Handler1Remote : Handler1, IRemote
    {
        public Remote Remote { get; init; }

        public Handler1Remote(Remote remote, IMediator mediator, ILogger<Handler1Remote> logger) : base(mediator, logger)
        {
            Remote = remote;
        }

        public override async Task<Response1> Handle(Request1 request, CancellationToken cancellationToken)
        {
            var ping = new Ping() { Message = request.Message };
            var pong = await this.SendRemote(ping, cancellationToken);
            return new Response1 { Message = pong.Message };
        }

        public override async Task Handle(Notification1 notification, CancellationToken cancellationToken)
        {
            await Task.CompletedTask; // handled by NotificationForwarder
        }
    }
}