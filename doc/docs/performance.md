# Performance

All performance measurements are based on an on a Intel(R) Xeon(R) CPU E3-1270
v5 @ 3.60GHz. As a baseline the performance of the gRPC client and server of the
official tutorial[^tut] without SSL being run single-threaded for 5 seconds is
about 3’400 requests/sec, configuration Debug vs. Release has virtually no
effect. The performance of DMediatR currently is considerably lower at about 40
requests/sec for unknown reasons.

The DMediatR `PerformanceMeters` uses `Ping` messages with a binary `Payload`
property which gets mirrored on the `Pong` reply to measure both compression and
decompression performance on the server.

## PerformanceMeter Test Setup

The `PerformanceMeter` implements `IRemote` to talk to a remote DMediatRNode:

[!code-csharp[PerformanceMeter.cs](../../test/DMediatR.Tests/Grpc/PerformanceMeter.cs?name=class)]

This enables this simple `SendRemote` extension method for the first request
which will negotiate SSL and only then establishes the connection:

[!code-csharp[PerformanceMeter.cs](../../test/DMediatR.Tests/Grpc/PerformanceMeter.cs?name=sendremote)]

The concrete `Remote` named "Ping" to connect to is configured in
appsettings.RemotePing.json for the test fixture:
 
[!code-json[appsettings.RemotePing.json](../../test/DMediatR.Tests/appsettings.RemotePing.json?name=remoteping)]

The one "Ping" Remote section is identical to the configured "Host" section of the server.


## Zero Payload

The 0 byte "overhead only" datapoint yields the mentioned 40 requests/sec when
run with 8 parallel tasks for 5 seconds:

```text
GrpcRaw Text Data: 0 B/Sec Pings: 41.80/Sec Count:216
```

## 10 MB payload

As the request overhead appears to be very high, the opposite extreme for the 10
MB payload datapoint is most relevant for evaluating the compression options.
As the defaults are lower, the MaxMessageSize must globally  be increased on
both the client and the server:

[!code-json[appsettings.json](../../src/DMediatRNode/appsettings.json?name=messagsize)]

The measurement is performed with three types of payloads: "Bloated" with a byte
array consisting only of zeros (Zero), "Compressible" with textual content
(Text) and "Incompressible" with only random bytes (Rand). The highest data
throughput rate when using no compression is due to the fact that network
bandwidth is not throttled.

### Without compression

The 10 MB datapoint yields the following result when using no compression - gRPC
seems to require a very long warm-up time:

```text
GrpcRaw Zero Data: 162.06 MB/Sec Pings: 15.46/Sec Count:80
GrpcRaw Text Data: 189.96 MB/Sec Pings: 18.12/Sec Count:96
GrpcRaw Rand Data: 196.03 MB/Sec Pings: 18.70/Sec Count:96
```

### gzip

Gzip with the grpc-accept-encoding header is enabled only server-side in
appsettings.Gzip.json:

[!code-json[appsettings.Gzip.json](../../src/DMediatRNode/appsettings.Gzip.json?name=gzip)]

```text
GrpcGzip Zero Data: 144.06 MB/Sec Pings: 13.74/Sec Count:72
GrpcGzip Text Data: 89.34 MB/Sec Pings: 8.52/Sec Count:48
GrpcGzip Rand Data: 77.59 MB/Sec Pings: 7.40/Sec Count:40
```

### LZ4

For using MessagePack's LZ4 compression instead of gRPC's Content-Encoding gzip,
it must be configured on both sides, here the server in
appsettings.Lz4BlockArray.json:

[!code-json[appsettings.Lz4BlockArray.json](../../src/DMediatRNode/appsettings.Lz4BlockArray.json?name=lz4)]

```text
GrpcLz4 Zero Data: 291.77 MB/Sec Pings: 27.83/Sec Count:144
GrpcLz4 Text Data: 148.18 MB/Sec Pings: 14.13/Sec Count:72
GrpcLz4 Rand Data: 164.1 MB/Sec Pings: 15.65/Sec Count:80
```    


[^tut]: [Tutorial: Create a gRPC client and server in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/tutorials/grpc/grpc-start?view=aspnetcore-8.0&tabs=visual-studio)
