using MessagePack;
using Microsoft.Extensions.DependencyInjection;

namespace DMediatR
{
    public interface ISerializer
    {
        byte[] Serialize(object obj);

        T Deserialize<T>(byte[] bytes);

        T Deserialize<T>(Type type, byte[] bytes);

        object Deserialize(Type type, byte[] bytes);
    }

    /// <summary>
    /// Pluggable binary serializer for DMediatR using MessagePackSerializer.Typeless as default.
    /// </summary>
    public class Serializer : ISerializer
    {
        private readonly IServiceProvider _serviceProvider;

        public Serializer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public virtual byte[] Serialize(object obj)
        {
            var customSerializer = _serviceProvider.GetKeyedService<ISerializer>(obj.GetType());
            if (customSerializer != null)
            {
                return customSerializer.Serialize(obj);
            }
            else
            {
                return MessagePackSerializer.Typeless.Serialize(obj);
            }
        }

        public virtual T Deserialize<T>(byte[] bytes)
        {
            return (T)Deserialize(typeof(T), bytes);
        }

        public virtual T Deserialize<T>(Type type, byte[] bytes)
        {
#pragma warning disable CA2263 // Generische Überladung bevorzugen, wenn der Typ bekannt ist
            return (T)Deserialize(typeof(T), bytes);
#pragma warning restore CA2263 // Generische Überladung bevorzugen, wenn der Typ bekannt ist
        }

        public virtual object Deserialize(Type type, byte[] bytes)
        {
            var customSerializer = _serviceProvider.GetKeyedService<ISerializer>(type);
            if (customSerializer != null)
            {
                return customSerializer.Deserialize(type, bytes);
            }
            else
            {
                return MessagePackSerializer.Typeless.Deserialize(bytes)!;
            }
        }
    }

    /// <summary>
    /// Generic custom binary serializer.
    /// </summary>
    /// <typeparam name="T">The concrete type to serialize.</typeparam>
    public class Serializer<T> : Serializer
    {
        public Serializer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Type of the serialized object as key for services.AddKeyedSingleton().
        /// </summary>
        public static Type Type => typeof(T);

        /// <summary>
        /// Throws an ArgumentException if the given object is not of type T.
        /// </summary>
        /// <param name="givenType"></param>
        /// <exception cref="ArgumentException"></exception>
        public void CheckType(Type givenType)
        {
            if (givenType != Type)
            {
                throw new ArgumentException(
                   $"""
                    {Type.Name} serializer used for type {givenType.Name}. Register the custom serializer exclusively with
                    services.AddKeyedSingleton<ISerializer, {this.GetType().Name}>({this.GetType().Name}.Type);
                    """);
            }
        }
    }
}