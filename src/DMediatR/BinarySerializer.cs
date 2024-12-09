using MessagePack;

namespace DMediatR
{
    /// <summary>
    /// Pluggable tiny wrapper around MessagePackSerializer.Typeless.
    /// </summary>
    internal class BinarySerializer : ISerializer
    {
        public byte[] Serialize(object obj)
        {
            return Serialize(obj.GetType(), obj);
        }

        public byte[] Serialize(Type _, object obj)
        {
            return MessagePackSerializer.Typeless.Serialize(obj);
        }

        public T Deserialize<T>(byte[] bytes)
        {
            return (T)Deserialize(typeof(T), bytes);
        }

        public object Deserialize(Type _, byte[] bytes)
        {
            return MessagePackSerializer.Typeless.Deserialize(bytes)!;
        }
    }
}