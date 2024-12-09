using Microsoft.Extensions.DependencyInjection;

namespace DMediatR
{
    /// <summary>
    ///  Internal serializer with explicit type indication.
    /// </summary>
    internal class TypedSerializer
    {
        private readonly IServiceProvider _serviceProvider;

        public TypedSerializer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Walk down the registered serializers chain until the base case <object> serializer is reached.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public byte[] Serialize(Type type, object obj)
        {
            (var registeredType, var serializer) = GetSerializer(type);
            if (registeredType.BaseType == null)
            {
                return serializer.Serialize(obj); // base case
            }
            else if (registeredType.IsAssignableFrom(type))
            {
                return serializer.Serialize(registeredType, obj); // custom serializer
            }
            else
            {
                return Serialize(registeredType.BaseType!, obj); // recursion
            }
        }

        /// <summary>
        /// Walk down the registered serializers chain until the base case <object> serializer is reached.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public object Deserialize(Type type, byte[] bytes)
        {
            (var registeredType, var serializer) = GetSerializer(type);
            if (registeredType.BaseType == null)
            {
                return serializer.Deserialize(registeredType, bytes);  // base case
            }
            else if (registeredType.IsAssignableFrom(type))
            {
                return serializer.Deserialize(registeredType, bytes); // concrete custom serializer
            }
            else
            {
                return Deserialize(registeredType.BaseType!, bytes); // recursion
            }
        }

        /// <summary>
        /// Recursively get the next matching concrete serializer for a type or one of its bases from DI.
        /// </summary>
        /// <param name="type">Type to serialize</param>
        /// <returns></returns>
        public (Type, ISerializer) GetSerializer(Type type)
        {
            var serializerFor = _serviceProvider.GetKeyedService<ISerializer>(type);
            if (serializerFor != null)
            {
                return (type, serializerFor); // base case
            }
            else if (type.BaseType != null)
            {
                return GetSerializer(type.BaseType); // recursion
            }
            else
            {
                throw new ArgumentException(
                         """
                      No recursion base case registered for type object with
                      services.AddKeyedSingleton<ISerializer, Serializer>(typeof(object));
                      """);
            }
        }
    }
}