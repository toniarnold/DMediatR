namespace DMediatR
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
            PreSerialize((T)obj);
        }

        public void PostDeserialize(object obj)
        {
            PostDeserialize((T)obj);
        }

        protected abstract void PreSerialize(T obj);

        protected abstract void PostDeserialize(T obj);
    }
}