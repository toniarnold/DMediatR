# Serializer

DMediatR uses binary serialization with pluggable custom serializers for specific types to serialize/deserialize,
here with the example of Ping deriving from a custom serializer which counts
the number of times the object has been serialized:

```plantUml
@startuml serialization-classes

Title Serialization Classes

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
    Serializer(type)
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


The corresponding sequence diagram hints at the serialization class dispatch mechanism:

```plantUml
@startuml serialization-sequence

Title Custom Serialization Sequence for Class Ping

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