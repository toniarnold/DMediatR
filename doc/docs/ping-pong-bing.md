# Ping-Pong and Bing

MediatR itself uses the classes `Ping` and `Pong`[^mediatr] as examples for `IRequest` request/response
messages that are dispatched to a single handler. For notification messages which get dispatched to
multiple handlers it also uses `Ping`. To avoid a name clash, DMediatR calls its built-in diagnosis
`INotification` message `Bing` for "Broadcast Ping".

In a distributed scenario, `Ping` gets sent to a specific remote node when configured so.
`Bing` messages get transitively forwarded to all configured remotes and the respective
remotes of the remote. It is the responsibility of the receiving `INotificationHandler` 
to decide whether that particular node is responsible for handling it.

## Hops Tracing

If a `Ping` message is handled locally, its message string remains unchanged as in plain MediatR.
But when it's handled remotely, it gets serialized and then deserialized at the receiving node
and vice versa the corresponding `Pong` response. As they are based on the 
`SerializationCountSerializer`[^serializer], the number of hops the message has taken gets counted
and appended to its message.

When a message is received from a remote, an information console log enry gets written
with tracing information:

The receiving node of a Ping sent with:

```csharp
await Mediator.Send(new Ping("from NUnit"));
```

... logs only the number of hops, which is always 1:

```text
info: DMediatR.PingHandler[0]
      Ping 1 hops from NUnit
```

The receiving node of a multi-hop Bing sent with:

```csharp
await Mediator.Publish(new Bing("from NUnit"));
```

... logs the number of hops and the nodes it has traversed:

```text
info: DMediatR.BingHandler[0]
      Bing 3 hops from NUnit via ClientCertifier via IntermediateCertifier via RootCertifier
```


[^mediatr]: [MediatR Wiki](https://github.com/jbogard/MediatR/wiki)

[^serializer]: [DMediatR Serializer](serializer.md)