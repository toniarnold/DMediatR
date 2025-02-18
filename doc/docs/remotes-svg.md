# /remotes.svg Graph

DMediatR nodes can generate SVG graph images for the configured remotes and
recursively for the remotes of the remotes under  `/remotes.svg`. This
functionality can be disabled with the `Grpc:RemotesSvg` setting if it is not
desired to expose the full topology of the network to all nodes.

The graphs are rendered using the Microsoft.Msagl.Drawing[^msagl] NuGet package.
The configuration of the rendering is done in C#. To customize and embellish it,
inherit from the `RemotesGraph` service, override its `GetSvg()` method and
replace the original class for `IRemotesGraph` in the service registration.

## appsettings.Monolith.json

The monolith without remotes from `ServerTest`[^ServerTest] produces just a
single box:

![remotes.Monolith.svg](../images/remotes.Monolith.svg)

## remotes.FourNodes.svg

The network with four nodes (not yet functional) from
`FourNodesTest`[^FourNodesTest] is recursively walked along and produces this
graph:

![remotes.FourNodes.svg](../images/remotes.FourNodes.svg)

[^msagl]: [Microsoft.Msagl.Drawing](https://www.nuget.org/packages/Microsoft.Msagl.Drawing)
    NuGet package for [Microsoft Automatic Graph Layout](https://github.com/microsoft/automatic-graph-layout)

[^ServerTest]: [ServerTest.cs](https://github.com/toniarnold/DMediatR/blob/main/test/DMediatR.Tests/Grpc/ServerTest.cs)

[^FourNodesTest]: [FourNodesTest.cs](https://github.com/toniarnold/DMediatR/blob/main/test/DMediatR.Tests/Grpc/FourNodesTest.cs)
