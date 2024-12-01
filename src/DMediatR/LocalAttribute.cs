using System.ComponentModel;

namespace DMediatR
{
    /// <summary>
    /// Marks the handlers in this class as local double of a [Remote] class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class LocalAttribute : Attribute
    {
        private readonly string _remoteName;

        public LocalAttribute(string remoteName)
        {
            _remoteName = remoteName;
        }

        /// <summary>
        /// The reference name matching the remote double of the class.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string? RemoteName(Type t) => GetLocalAttribute(t)?._remoteName;

        private static LocalAttribute? GetLocalAttribute(Type t)
        {
            return (LocalAttribute?)(from Attribute a in TypeDescriptor.GetAttributes(t)
                                     where a is LocalAttribute
                                     select a).FirstOrDefault();
        }
    }
}