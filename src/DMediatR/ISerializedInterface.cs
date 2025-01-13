﻿namespace DMediatR
{
    /// <summary>
    /// Interface for adding concrete SerializedInterface&lt;T&gt; classes with
    /// services.TryAddKeyedSingleton()
    /// </summary>
    public interface ISerializedInterface
    {
        public void PreSerialize(object obj);

        public void PostDeserialize(object obj);
    }

    /// <summary>
    /// Interface custom serialization is orthogonal to a CustomSerializer class
    /// hierarchy: Serializable classes can implement multiple interfaces, which
    /// in turn require e.g. specific members which must be dehydrated before
    /// serialization.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SerializedInterface<T> : ISerializedInterface
    {
        public void PreSerialize(object obj)
        {
            CheckType(obj);
            PreSerialize((T)obj);
        }

        public void PostDeserialize(object obj)
        {
            CheckType(obj);
            PostDeserialize((T)obj);
        }

        protected virtual void PreSerialize(T obj)
        {
        }

        protected virtual void PostDeserialize(T obj)
        {
        }

        /// <summary>
        /// Throws an ArgumentException if the given object is not derived of type T of the generic class.
        /// </summary>
        /// <param name="givenType"></param>
        /// <exception cref="ArgumentException"></exception>
        private void CheckType(object obj)
        {
            foreach (var @interface in obj.GetType().GetInterfaces())
            {
                var tocheck = @interface;
                while (true)
                {
                    if (tocheck == typeof(T)) return; // OK, found
                    if (tocheck.BaseType == null) break; // try next interface
                    tocheck = tocheck.BaseType;
                }
            }
            throw new ArgumentException(
               $"""
                {typeof(T).Name} SerializedInterface used for type {obj.GetType().Name}. Register the interface exclusively with
                services.AddKeyedSingleton<ISerializedInterface, {this.GetType().Name}>(typeof({typeof(T).Name}));
                """);
        }
    }
}