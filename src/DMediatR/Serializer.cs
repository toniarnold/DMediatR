using Microsoft.Extensions.DependencyInjection;

namespace DMediatR
{
    /// <summary>
    /// Interface for the base serializer and all specialized serializers.
    /// Use it for injecting an alternate base BinarySerializer implementation with
    /// services.AddKeyedSingleton&lt;ISerializer, BinarySerializer&gt;(typeof(object));
    /// Its pluggable default implementation is a tiny wrapper around MessagePackSerializer.Typeless.
    /// </summary>
    public interface ISerializer
    {
        byte[] Serialize(object obj);

        byte[] Serialize(Type type, object obj);

        T Deserialize<T>(byte[] bytes);

        object Deserialize(Type type, byte[] bytes);
    }

    /// <summary>
    /// General serializer gathering required specialized serializers from the ServiceCollection.
    /// </summary>
    public class Serializer : ISerializer
    {
        private readonly TypedSerializer _typedSerializer;

        public Serializer(IServiceProvider serviceProvider)

        {
            _typedSerializer = serviceProvider.GetRequiredService<TypedSerializer>();
        }

        public byte[] Serialize(object obj)
        {
            return Serialize(obj.GetType(), obj);
        }

        public virtual byte[] Serialize(Type type, object obj)
        {
            return _typedSerializer.Serialize(type, obj);
        }

        public T Deserialize<T>(byte[] bytes)
        {
            return (T)Deserialize(typeof(T), bytes);
        }

        public virtual object Deserialize(Type type, byte[] bytes)
        {
            var obj = _typedSerializer.Deserialize(type, bytes);
            return (dynamic)obj!;
        }
    }
}