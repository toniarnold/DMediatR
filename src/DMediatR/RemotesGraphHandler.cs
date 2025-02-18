using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Msagl.Drawing;

namespace DMediatR
{
    internal class RemotesGraphHandler : IRequestHandler<RemotesGraphRequest, RemotesGraphRequest>, IRemote
    {
        private readonly HostOptions _host;
        private readonly RemotesOptions _remotes;
        private readonly GrpcOptions _grpcOptions;
        private readonly IMemoryCache _cache;
        private readonly Remote _remote;

        public Remote Remote => _remote;

        public RemotesGraphHandler(
            IOptions<HostOptions> hostOptions,
            IOptions<RemotesOptions> remotesOptions,
            IOptions<GrpcOptions> grpcOptions,
            IMemoryCache cache,
            Remote remote)
        {
            _host = hostOptions.Value;
            _remotes = remotesOptions.Value;
            _grpcOptions = grpcOptions.Value;
            _cache = cache;
            _remote = remote;
        }

        public async Task<RemotesGraphRequest> Handle(RemotesGraphRequest request, CancellationToken cancellationToken)
        {
            if (!_cache.HaveSeen(request.CorrelationGuid, _grpcOptions.MaxLatency))
            {
                // First add the node itself
                var hostName = $"{_host.Host}:{_host.Port}";
                request.Nodes.Add(new RemotesGraphRequest.Node(hostName));
                foreach (var remote in _remotes)
                {
                    // Then add the configured remote node
                    var remoteName = $"{remote.Value.Host}:{remote.Value.Port}";
                    request.Nodes.Add(new RemotesGraphRequest.Node(remoteName));
                    request.Edges.Add(new RemotesGraphRequest.Edge(hostName, remote.Key, remoteName));

                    // At last add the remotes of each remote depth-first
                    // updating the request object on traversal
                    if (request.Recursive)
                    {
                        request = await this.SendRemote(remote.Value, request, cancellationToken);
                    }
                }
            }

            return await Task.FromResult(request);
        }
    }
}