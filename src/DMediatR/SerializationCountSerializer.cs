namespace DMediatR
{
    public class SerializationCountSerializer : CustomSerializer<SerializationCountMessage>
    {
        public SerializationCountSerializer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Before serializing increment the object's Count property.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override byte[] Serialize(Type type, object obj)
        {
            CheckType(type);
            ((SerializationCountMessage)obj).Count++;
            return base.Serialize(type, obj, checkType: false);
        }
    }
}