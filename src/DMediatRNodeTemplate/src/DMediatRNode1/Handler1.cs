namespace DMediatRNode1
{
    [Local("Handler1")]
    public class Handler1 : IRequestHandler<Request1, Response1>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<Handler1Remote> _logger;

        public Handler1(IMediator mediator, ILogger<Handler1Remote> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public virtual async Task<Response1> Handle(Request1 request, CancellationToken cancellationToken)
        {
            var ping = new Ping() { Message = request.Message };
            var pong = await _mediator.Send(ping);
            return new Response1 { Message = pong.Message };
        }

        public virtual async Task Handle(Notification1 notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{notification}: {msg}", notification.GetType().Name, notification.Message);
            await Task.CompletedTask;
        }
    }
}