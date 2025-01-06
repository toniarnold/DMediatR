namespace DMediatR
{
    /// <summary>
    /// Generic custom binary serializer to inherit from when implementing a concrete custom serializer
    /// for a specific type T. Types deriving from T will also use that serializer.
    /// </summary>
    /// <typeparam name="T">The concrete type to serialize.</typeparam>
    public abstract class CustomSerializer<T> : Serializer, ISerializer
    {
        public CustomSerializer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Use this type of the serialized object as key for services.AddKeyedSingleton().
        /// </summary>
        public static Type Type => typeof(T);

        public override byte[] Serialize(Type type, object obj)
        {
            return Serialize(type, obj, checkType: true);
        }

        public byte[] Serialize(Type type, object obj, bool checkType = true)
        {
            if (checkType) CheckType(type);
            return base.Serialize(type.BaseType!, obj);
        }

        public override object Deserialize(Type type, byte[] bytes)
        {
            var obj = Deserialize(type, bytes, checkType: true);
            return obj;
        }

        protected object Deserialize(Type type, byte[] bytes, bool checkType)
        {
            if (checkType) CheckType(type);
            return base.Deserialize(type.BaseType!, bytes);
        }

        /// <summary>
        /// Throws an ArgumentException if the given object is not derived of type T of the generic class.
        /// </summary>
        /// <param name="givenType"></param>
        /// <exception cref="ArgumentException"></exception>
        protected void CheckType(Type givenType)
        {
            Type type = givenType;
            while (true)
            {
                if (type == Type) return; // OK, found
                if (type.BaseType == null) break; // object, thus throw as not found
                type = type.BaseType;
            }
            throw new ArgumentException(
               $"""
                {Type.Name} serializer used for type {givenType.Name}. Register the custom serializer exclusively with
                services.AddKeyedSingleton<ISerializer, {this.GetType().Name}>({this.GetType().Name}.Type);
                """);
        }
    }
}