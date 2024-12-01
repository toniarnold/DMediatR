namespace DMediatR.Tests.MediatR.LocalRemote
{
    public class Ping : IRequest<Pong>
    {
        public string? Message { get; set; }
    }
}