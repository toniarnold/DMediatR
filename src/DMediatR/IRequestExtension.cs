namespace DMediatR
{
    internal static class IRequestExtension
    {
        public static Type? GetResponseType(this IBaseRequest request)
        {
            return (from i in request.GetType().GetInterfaces()
                    where i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IRequest<>)
                    select i.GetGenericArguments()[0]).FirstOrDefault();
        }
    }
}