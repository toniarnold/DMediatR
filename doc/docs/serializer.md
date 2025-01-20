# Serializer

DMediatR uses typed binary serialization with pluggable custom serializers to
transmit MediatR `IRequest`/`IResponse` messages over gRPC. Custom serializers
for specific types can be added to the service collection.  There are two
internal custom serializers: One for serializing `X509Certificate2` objects and
one for tracing purposes counting the number of times the object has been
serialized.


## Injecting Custom Serializers

DMediatR leverages keyed dependency injection, introduced in .NET 8, to look up
custom serializers for a particular type. This excerpt from the
`ServiceCollectionExtension` registers, along with the required infrastructure,
the two custom serializers `SerializationCountSerializer` and
`X509CertificateSerializer`:

[!code-csharp[ServiceCollectionExtension.cs](../../src/DMediatR/ServiceCollectionExtension.cs?name=registerserializers&highlight=4-6)]

The `ILockSerializedInterface` is a special case, as it is declared for the
`ILock` interface and not a concrete class, as multiple class hierarchies can
implement the same interface.


## Custom Serializer Implementation

Custom serializers inherit from the generic class `CustomSerializer<T>` with the class to
register the serializer for as type parameter. They can override one or both of the
`Serialize` and `Deserialize` methods. This can be used e.g. for dehydrating an object by
nulling out non-serializable members before serialization and then for rehydrating it after 
deserialization by setting the members again with instances from DI on the destination node.

The `SerializationCountSerializer` is used to trace the number of node hops a
DMediatR message (IRequest or INotification) has taken by incrementing the
object's Count property:

[!code-csharp[SerializationCountSerializer.cs](../../src/DMediatR/SerializationCountSerializer.cs)]

The `X509CertificateSerializer` needs the injected password from configuration
to decrypt the .pfx binary for deserialization and uses plain `byte[]`
serialization for the data exported by the `X509Certificate2` object:

[!code-csharp[X509CertificateSerializer.cs](../../src/DMediatR/X509CertificateSerializer.cs)]

When de- resp. rehydrating is required by an interface requiring a
non-serializable member, a `CustomSerializer<T>` based on a class hierarchy is
not appropriate, as interface custom serialization is orthogonal to the class
hierarchy: Serializable classes can implement multiple interfaces, which in turn
require e.g. specific members which must be dehydrated before serialization for
each interface implemented. 

The `ILockSerializedInterface` is defined for the
interface `ILock` and overrides the two `Dehydrate`/`Rehydrate` hooks
called by the general `Serializer` class:

[!code-csharp[ILockISerializedInterface.cs](../../src/DMediatR/ILockISerializedInterface.cs)]


## Serialization Classes

### Context for serializing `Ping` objects

This diagram exemplifies the context for serializing of the `Ping` class
deriving from `SerializationCountSerializer`:

```plantUml
@startuml serialization-classes

' Serializer
interface ISerializer {
    Serialize(obj)
    Serialize(type, obj)
    Deserialize<T>(bytes)
    Deserialize(type, bytes)
}

class Serializer implements ISerializer {
    - _typedSerializer
    Serialize(obj)
    Serialize(type, obj)
    Deserialize<T>(bytes)
    Deserialize(type, bytes)
}

class BinarySerializer implements ISerializer {
    Serialize(obj)
    Serialize(type, obj)
    Deserialize<T>(bytes)
    Deserialize(type, bytes)
}

class MessagePackSerializer.Typeless {
    Serialize(obj)
    Deserialize(bytes)
}

BinarySerializer --> Typeless

class TypedSerializer {
    - _serviceProvider
    Serialize(type, obj)
    Deserialize(type, bytes)
    GetSerializer(type)
}

Serializer *-- TypedSerializer

class CustomSerializer<T> extends Serializer implements ISerializer{
    + Type
    # CheckType(givenType)
}

class SerializationCountSerializer extends CustomSerializer {
    Serialize(type, obj)
}


' Serialized Objects

interface IRequest<TResponse>

interface INotification


abstract class SerializationCountMessage  {
    + Count
}

class Ping extends SerializationCountMessage implements IRequest {
    + Message
}

class Pong extends SerializationCountMessage implements IRequest {
    + Message
}

class Bing extends SerializationCountMessage implements INotification {
    + Message
}

@enduml

```

### Serialization Sequence for `Ping`

The corresponding sequence diagram hints at the serialization class dispatch mechanism:

```plantUml
@startuml serialization-sequence

participant RemoteExtension
participant Serializer
participant TypedSerializer
participant SerializationCountSerializer
participant BinarySerializer


RemoteExtension -> Serializer: Serialize(obj)
activate Serializer
Serializer -> Serializer: Serialize(type, obj)
Serializer -> TypedSerializer: Serialize(type, obj)
activate TypedSerializer
TypedSerializer -> TypedSerializer: GetSerializer(type)
TypedSerializer -> SerializationCountSerializer: Serialize(type, obj)
activate SerializationCountSerializer
SerializationCountSerializer -> SerializationCountSerializer: GetSerializer(type)
SerializationCountSerializer -> Serializer: Serialize(type, obj)
Serializer -> TypedSerializer: Serialize(type, obj)
TypedSerializer -> TypedSerializer: GetSerializer(type)
TypedSerializer -> BinarySerializer: Serialize(type, obj)
activate BinarySerializer
BinarySerializer --> SerializationCountSerializer: byte[]
deactivate BinarySerializer
SerializationCountSerializer --> TypedSerializer: byte[]
deactivate SerializationCountSerializer
TypedSerializer --> Serializer: byte[]
deactivate TypedSerializer
Serializer --> RemoteExtension: byte[]
deactivate Serializer

@enduml
```

