# DMediatR

**D**istributed [MediatR](https://github.com/jbogard/MediatR) over gRPC with
auto-renewing X509 client certificate TLS.

Distribute an existing MediatR monolith across microservice nodes by inserting
a `D` in its `services.AddMediatR()` call and, after deployment, configure in
each appsettings.json whether messages will be handled locally or are delegated
to a specified node. If nothing is configured, the monolith works as before.

For transmission over gRPC, MediatR messages are transparently binary serialized
using [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp).
The serialization can be customized for specific object types or interfaces.

Validation of the generated X509 certificate chain ignores hostnames, it only
validates that the client and server certificates match. It is therefore
sufficient for the destination nodes to be reachable by DNS name or IP address,
just as for SSH, which is required to deploy the initial certificate chain.

DMediatR nodes can also be deployed on Linux ARM
[IoT](https://toniarnold.github.io/DMediatR/docs/iot.html) devices such as a
Raspberry PI 3+. This allows them to interoperate seamlessly with Windows .NET
via DMediatR.

[Documentation](https://toniarnold.github.io/DMediatR/) 
([C# API](https://toniarnold.github.io/DMediatR/api/DMediatR.html))

NuGet Packages: [DMediatR](https://www.nuget.org/packages/DMediatR)
[Template](https://www.nuget.org/packages/DMediatRNodeTemplate)
