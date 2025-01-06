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
        protected readonly IServiceProvider _serviceProvider;
        private readonly TypedSerializer _typedSerializer;

        public Serializer(IServiceProvider serviceProvider)

        {
            _serviceProvider = serviceProvider;
            _typedSerializer = serviceProvider.GetRequiredService<TypedSerializer>();
        }

        public byte[] Serialize(object obj)
        {
            PreSerialize(obj);
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
            PostDeserialize(obj);
            return (dynamic)obj!;
        }

        private void PreSerialize(object obj)
        {
            foreach (var @interface in obj.GetType().GetInterfaces())
            {
                var processorFor = GetSerializedInterface(@interface);
                processorFor?.PreSerialize(obj);
            }
        }

        private void PostDeserialize(object obj)
        {
            foreach (var @interface in obj.GetType().GetInterfaces())
            {
                var processorFor = GetSerializedInterface(@interface);
                processorFor?.PostDeserialize(obj);
            }
        }

        /// <summary>
        /// Recursively get the next matching concrete serializer
        /// pre-/post-processor for an interface or one of its bases from DI.
        /// </summary>
        /// <param name="type">Type to serialize</param>
        /// <returns></returns>
        private ISerializedInterface? GetSerializedInterface(Type type)
        {
            var processorFor = _serviceProvider.GetKeyedService<ISerializedInterface>(type);
            if (processorFor != null)
            {
                return processorFor; // base case 1: found
            }
            else if (type.BaseType != null)
            {
                return GetSerializedInterface(type.BaseType); // recursion
            }
            else
            {
                return null; // base case 2: not found, no processing
            }
        }
    }
}