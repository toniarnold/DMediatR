namespace DMediatR
{
    [Local("Ping")]
    internal class PingHandler : IRequestHandler<Ping, Pong>
    {
        public virtual async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            var env = Environment.GetEnvironmentVariables();
            //foreach (var key in env.Keys)
            //{
            //    Console.WriteLine($"{key}:{env[key]}");
            //}
            var from = env.Contains("ASPNETCORE_ENVIRONMENT") ? $" from {(string)env["ASPNETCORE_ENVIRONMENT"]!}" : "";
            await Task.CompletedTask;
            return new Pong { Message = $"{request.Message}{from}" };
        }
    }
}