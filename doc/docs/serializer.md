# Serializer

DMediatR uses binary serialization with pluggable custom serializers for specific types to serialize/deserialize,
here with the example of Ping deriving from a custom serializer which counts
the number of times the object has been serialized.

## Serialization Classes

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

class MessagePackSerializer.Typeless

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


class SerializationCountMessage  {
    + Count
}

class Ping extends SerializationCountMessage implements IRequest {
    + Message
}

@enduml
```

## Custom Serialization Sequence for Class Ping

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

## Injecting Custom Serializers

DMediatR leverages keyed dependency injection introduced in .NET 8 to look up 
custom serializers for a particular type. This excerpt from the
`ServiceCollectionExtension` registers along with the required infrastructure 
the two custom serializers `SerializationCountSerializer` and `X509CertificateSerializer`:

[!code-csharp[](../../src/DMediatR/ServiceCollectionExtension.cs#registerserializers)]

### Custom Serializer Implementation

Custom serializers inherit from the generic class `CustomSerializer<T>` with the class to
register the serializer for as type parameter. They can override one or both of the
`Serialize` and `Deserialize` methods. This can be used e.g. for dehydrating an object by
nulling out non-serializable members before serialization and then for rehydrating it after 
deserialization by setting the members again with instances from DI on the destination node.

The `SerializationCountSerializer` is used to trace the number of node hops a DMediatR 
message (IRequest or INotification) has taken:

[!code-csharp[](../../src/DMediatR/SerializationCountSerializer.cs)]

The `X509CertificateSerializer` needs the injected password from configuration to decrypt
the .pfx binary for both operations and uses plain `byte[]` serialization for it:

[!code-csharp[](../../src/DMediatR/X509CertificateSerializer.cs)]