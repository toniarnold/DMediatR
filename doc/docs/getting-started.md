# Getting Started

## Install and run the DMediatRNodeTemplate

To get started quickly, install the current DMediatRNodeTemplate[^template]
NuGet template with the .NET CLI command with the exact preview version shown on
its webpage. Then, in an empty folder, create the solution e.g. with the name:

```ps
dotnet new  DMediatRNode -n Grpc
```

This template creates the following directory structure:

```plaintext
cert/            ← Directory for the generated X509 certificates
src/Grpc/        ← DMediatR gRPC node project
test/Grpc.Test/  ← Integration tests for above project
Grpc.sln         ← Solution file for VS 2022
README.md
start.ps1        ← Script for starting up gRPC nodes by launchSettings profile names
test.ps1         ← Script for running a test by NUnit Test Selection Language
```

Open and build the solution. Open the Test-Explorer[^explorer] and run all
tests. Several .NET process windows will pop up and disappear quickly.

## Inspect the console output

To view the console log, prevent the process windows from closing by setting a
breakpoint on `SetUp.StopAllServers();` e.g. in the `PingRedBlueTest.cs` test
file, as this uses two nodes Red and Blue, which can be identified by the window
title (the "RedPingBlue" combo indicates that the Red node is configured to
forward `Ping` to Blue):

```plaintext
DMediatR RedPingBlue on localhost:18005
```
```plaintext
DMediatR Blue on localhost:18003
```

Ignore the X509 certificate warnings from AspNetCore itself, as DMediatR uses a
self-signed root certificate[^wcf] that can't be verified:

```plaintext
warn: Microsoft.AspNetCore.Authentication.Certificate.CertificateAuthenticationHandler[2]
      Certificate validation failed, subject was CN=ClientCertifier. NotSignatureValid (...)
```

The interesting part comes after the certificate warning with the DMediatR
tracing Ping-Pong[^pingpong]:

### Red
```plaintext
info: DMediatR.BingHandler[0]
      Handling Bing from NUnit
dbug: DMediatR.NotificationForwarder[0]
      Handling/Forwarding Bing
info: DMediatR.NotificationForwarder[0]
      Handling/Forwarding Bing Bing 1 hops from NUnit via RedPingBlue
dbug: DMediatR.Remote[0]
      Forwarding Bing to localhost:18003
info: DMediatR.BingHandler[0]
      Handling Bing 1 hops from NUnit via RedPingBlue
dbug: DMediatR.NotificationForwarder[0]
      Handling/Forwarding Bing
info: DMediatR.NotificationForwarder[0]
      Handling/Forwarding Bing Bing 2 hops from NUnit via RedPingBlue via RedPingBlue
dbug: DMediatR.Remote[0]
      Forwarding Bing to localhost:18003
dbug: DMediatR.Remote[0]
      Send Ping to https://localhost:18003/
info: DMediatR.PingHandlerRemote[0]
      Pong 3 hops from NUnit via RedPingBlue via Blue via RedPingBlue
```

### Blue
```plaintext
info: DMediatR.BingHandler[0]
      Handling Bing 1 hops from NUnit via RedPingBlue
dbug: DMediatR.NotificationForwarder[0]
      Handling/Forwarding Bing
info: DMediatR.NotificationForwarder[0]
      Handling/Forwarding Bing Bing 2 hops from NUnit via RedPingBlue via Blue
info: DMediatR.BingHandler[0]
      Handling Bing 2 hops from NUnit via RedPingBlue via Blue
dbug: DMediatR.NotificationForwarder[0]
      Handling/Forwarding Bing
info: DMediatR.NotificationForwarder[0]
      Handling/Forwarding Bing Bing 2 hops from NUnit via RedPingBlue via Blue via Blue
info: DMediatR.PingHandler[0]
      Ping 2 hops from NUnit via RedPingBlue via Blue
      Pong 3 hops from NUnit via RedPingBlue via Blue via RedPingBlue
```

The `Bing` broadcast MediatR `INotification` published by the NUnit test runner
first reaches Red on the `BingHandler` ("Handling Bing from NUnit") and is
immediately forwarded by the `NotificationForwarder` to Blue ("Forwarding Bing
to localhost:18003"), which receives it on the `BingHandler` ("Handling Bing 1
hops from NUnit via RedPingBlue").

The `Ping` message is configured to be sent from the NUnit test-runner directly
to Red, which in turn is configured to forward it to Blue ("Forwarding Bing to
localhost:18003"). After being routed back to the NUnit test-runner, the message
is logged as having taken 4 hops, as can be seen in the VS Test output window:

```plaintext
Information: Pong 4 hops from NUnit via RedPingBlue via Blue via RedPingBlue via localhost:8081
```

Instead of setting a breakpoint, the `start.ps1` resp `test.ps1` scripts can be
used as explained in the debugging chapter[^debugging].


[^template]: [DMediatRNodeTemplate NuGet package](https://www.nuget.org/packages/DMediatRNodeTemplate)

[^explorer]: [https://learn.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer?view=vs-2022#run-tests-in-test-explorer](https://learn.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer?view=vs-2022#run-tests-in-test-explorer)

[^wcf]: DMediatR uses the same trust model as ancient WCF 4.0: In that framework
at that time, a self-signed server certificate was disallowed as insecure, but
as soon as a client certificate was also required, WCF accepted the whole as
secure enough. In DMediatR, trust is built on the intermediate
certificate: If a responding server's certificate was issued by the same
intermediate certifier, it is considered trustworthy. The server in turn
validates the required client certificate against the intermediate certificate.

[^pingpong]: [DMediatR Ping-Pong and Bing](ping-pong-bing.md)

[^debugging]: [DMediatR Debugging](debugging.md)
