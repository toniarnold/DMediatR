﻿# Ping-Pong and Bing

MediatR itself uses the classes `Ping` and `Pong`[^mediatr] as examples for
`IRequest` request/response messages that are dispatched to a single handler. It
also uses `Ping` for notification messages sent to multiple handlers. To
distinguish it, DMediatR calls its built-in diagnostic `INotification` message 
`Bing` for "Broadcast Ping".

In a distributed scenario, `Ping` gets sent to a specific remote node when
configured so. `Bing` messages get transitively forwarded to all configured
remotes and the respective remotes of the remote. It is the responsibility of
the receiving `INotificationHandler` to decide whether that particular node is
responsible for handling it.

## Hops Tracing

When a `Ping` message is handled locally, its message string remains unchanged
as in plain MediatR. But when it's handled remotely, it gets serialized and then
deserialized at the receiving node and vice versa the corresponding `Pong`
response. As they are based on the `SerializationCountSerializer`[^serializer],
the number of hops the message has taken gets counted and appended to its
message.

When a message is received from a remote node, an information console log entry
gets written with tracing information:

### Ping

The receiving node of a Ping sent with:

```csharp
await Mediator.Send(new Ping("from NUnit"));
```

... logs the number of hops, which is always 1 on the remote node:

```text
info: DMediatR.PingHandler[0]
      Ping 1 hops from NUnit
```

... and 2 on the calling node which also logs the name of the remote node:

```text
info: DMediatR.PingHandlerRemote[0]
      Pong 2 hops from NUnit via ClientCertifier
```

### Bing

The receiving node of a multi-hop Bing sent with:

```csharp
await Mediator.Publish(new Bing("from NUnit"));
```

... logs the number of hops and the nodes it has traversed:

```text
info: DMediatR.NotificationForwarder[0]
      Forwarding Bing 2 hops from NUnit via ClientCertifier via IntermediateCertifier
```

[^mediatr]: [MediatR Wiki](https://github.com/jbogard/MediatR/wiki)

[^serializer]: [DMediatR Serializer](serializer.md)