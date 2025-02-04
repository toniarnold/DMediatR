# IoT

The IoT use case is an example of the way DMediatR is opinionated: Encourage the
distribution of the same monolith across all nodes of a microservice
architecture - it is the responsibility of the RPC caller to use only a node that
actually provides the desired service. In this case, querying the CPU
temperature on a Windows machine throws the exception "Iot.Device.CpuTemperature
is not available on this device".

## The Iot sample node

The `Iot` sample host can be published from VS with the RuntimeIdentifier
`linux-arm64`[^rpi3]. The included `limux-arm64.pubxml` in its
`.\Properties\PublishProfiles` points to an SMB share `Z:` connected via USB to
the target Raspberry PI. After deployment, it can be started with `./Iot`.

### The developer certificate

> [!IMPORTANT] 
> Although it is in fact not used at all, the dynamic late certificate
> assignment causes the server to require a developer certificate installed during startup.
> To avoid having to install the dotnet SDK[^dotnet] despite publishing it as self-contained
> app[^selfcontained], you can copy a developer certificate manually to its directory created with 
> > `mkdir -p ~/.dotnet/corefx/cryptography/x509stores/my`

### The smoke test

The `appsettings.Iot.json` in `DMediatR.Tests` points to the Raspberry PI host
named `rpi`. It is written with a trailing dot to force Windows to actually
query the DNS server (a Pi-hole with its `/etc/hosts`).

The certificate path in the `DMediatR:Certificate:FilePath` points to the `cert`
directory in the IoT application. If you create the initial certificate chain on
the Raspberry Pi with `.\Iot init`, manually copy it over to that directory.

The first of the two test methods in `IotTest.cs` simply sends a GET request
like a web browser would and asserts having received the same response:

[!code-csharp[IotTest.cs](../../test/DMediatR.Tests/Grpc/IotTest.cs?name=cputemp)]

The second one actually uses the DMediatR infrastructure and sends a serialized
MediatR `IRequest` to query the CPU temperature of the Raspberry PI using the
`Iot.Device.Bindings` NuGet package[^bindings]. Then, for plausibility, this
temperature can manually be compared to what e.g. the PI-hole admin page
reports:

```text
 CpuTemp of https://rpi.:18001/ is 46.7 ℃.
```


[^rpi3]: It's been a long way  from the days when the special Windows
    distribution designed  to run .NET on a Raspberry PI 3 refused to boot on a
    3+ because of the "+"…

[^dotnet]: `./dotnet-install.sh --channel 8.0` with a [scripted install](https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#scripted-install)

[^selfcontained]: [Deploying a self-contained app](https://learn.microsoft.com/en-us/dotnet/iot/deployment#deploying-a-self-contained-app)

[^bindings]: [Iot.Device.Bindings](https://www.nuget.org/packages/Iot.Device.Bindings/)