# IoT

The IoT use case is an example of the way DMediatR is opinionated: Encourage the
distribution of the same monolith across all nodes of a microservice
architecture - it is the responsibility of the RPC caller to use only a node that
actually provides the desired service. In this case, querying the CPU
temperature on a Windows machine throws the exception "Iot.Device.CpuTemperature
is not available on this device".

## The Iot sample node

The `Iot` sample host can be published from VS with the RuntimeIdentifier
`linux-arm64`[^rpi3]. The included
[limux-arm64.pubxml](https://github.com/toniarnold/DMediatR/blob/main/src/Iot/Properties/PublishProfiles/limux-arm64.pubxml)
points to an SMB share `Z:` connected via USB to the target Raspberry PI. After
deployment, it can be started there locally with `./Iot`.

### The developer certificate

> [!IMPORTANT] 
> Although it is in fact not used at all, the dynamic late certificate
> assignment causes the server to require a developer certificate installed during startup.
> To avoid having to install the dotnet SDK[^dotnet] despite publishing it as self-contained
> app[^selfcontained], you can copy a developer certificate manually to its directory created with 
> > `mkdir -p ~/.dotnet/corefx/cryptography/x509stores/my`

### The smoke test

The
[appsettings.Iot.json](https://github.com/toniarnold/DMediatR/blob/main/test/DMediatR.Tests/appsettings.Iot.json)
in `DMediatR.Tests` points to the Raspberry PI host named `rpi`. It is written
with a trailing dot to force Windows to actually query the Linux DNS server assigned by DHCP:

[!code-javascript[IotTest.cs](../../test/DMediatR.Tests/appsettings.Iot.json?name=remotes)]

The certificate path in the `DMediatR:Certificate:FilePath` points to the `cert`
directory in the Iot application. If you create the initial certificate chain on
the Raspberry Pi with `./Iot init`, manually copy it over to that directory.

The first of the two test methods in
[IotTest.cs](https://github.com/toniarnold/DMediatR/blob/main/test/DMediatR.Tests/Grpc/IotTest.cs)
simply sends a GET request like a web browser would and asserts having received
the same response:

[!code-csharp[IotTest.cs](../../test/DMediatR.Tests/Grpc/IotTest.cs?name=cputemp)]

The second one actually uses the DMediatR infrastructure and sends a serialized
MediatR `IRequest` to remotely query the CPU temperature of the Raspberry PI
using the `Iot.Device.Bindings` NuGet package[^bindings]. On the PI, the request
is handled locally in the
[TempHandler.cs](https://github.com/toniarnold/DMediatR/blob/main/src/Iot/TempHandler.cs)
and the result is sent back. Then, for plausibility, this temperature can
manually be compared to what e.g. `vcgencmd measure_temp` in an SSH shell reports:

```text
 CpuTemp of https://rpi.:18001/ is 46.7 ℃.
```

### Monolithic /remotes.svg Graph

In above configuration, the /remotes.svg graph produced by the `Iot` node
contains just the node itself:

![remotes.Iiot.svg](../images/remotes.Iot.svg)


## Splitting up  the monolith

By simply amending a `Remotes` section to the `appsettings.json`, the monolith
can be split up into two microservices: The former monolith handles the
`TempRequest` as before, but transparently forwards it to the designated second
node called `rpi2` (this time without the trailing dot, as the DNS is queried on
Linux):

``` javascript
    "Remotes": {
      "CpuTemp": {
        "Host": "rpi2",
        "Port": 18001,
        "OldPort": 18002
      }
    }
```

When the `GetRemoteTemp` test is run again, it suddenly fails the plausibility
test (the first one is a PI 4, the second one a PI 5):

```text
 CpuTemp of https://rpi.:18001/ is 56.2 ℃.
```

### Two nodes /remotes.svg Graph

The graph produced by the original `rpi` node now displays the second node:

![remotes.Iot2.svg](../images/remotes.Iot2.svg)



[^rpi3]: It's been a long way  from the days when the special Windows
    distribution designed  to run .NET on a Raspberry PI 3 refused to boot on a
    3+ because of the "+"…

[^dotnet]: `./dotnet-install.sh --channel 8.0` with a [scripted install](https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#scripted-install)

[^selfcontained]: [Deploying a self-contained app](https://learn.microsoft.com/en-us/dotnet/iot/deployment#deploying-a-self-contained-app)

[^bindings]: [Iot.Device.Bindings](https://www.nuget.org/packages/Iot.Device.Bindings/)