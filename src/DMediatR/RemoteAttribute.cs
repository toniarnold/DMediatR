using System.ComponentModel;

namespace DMediatR
{
    /// <summary>
    /// Marks the handlers in this class as calling a gRPC endpoint (as opposed to calling locally).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class RemoteAttribute : Attribute
    {
        private readonly string _name;

        public RemoteAttribute(string name)
        {
            _name = name;
        }

        /// <summary>
        /// The reference name to be used in the "Remotes" configuration section.
        /// </summary>
        public static string? Name(Type t) => GetRemoteAttribute(t)?._name;

        private static RemoteAttribute? GetRemoteAttribute(Type t)
        {
            return (RemoteAttribute?)(from Attribute a in TypeDescriptor.GetAttributes(t)
                                      where a is RemoteAttribute
                                      select a).FirstOrDefault();
        }
    }
}