namespace DMediatR
{
    internal class SerializationCountSerializer : CustomSerializer<SerializationCountMessage>
    {
        public SerializationCountSerializer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override byte[] Serialize(Type type, object obj)
        {
            CheckType(type);
            ((SerializationCountMessage)obj).Count++;
            return base.Serialize(type, obj, checkType: false);
        }
    }
}