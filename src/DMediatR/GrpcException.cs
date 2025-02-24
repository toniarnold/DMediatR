using Google.Rpc;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMediatR
{
    /// <summary>
    /// Custom exception which extracts and returns the remote stack trace from
    /// the Details Metadata of a non-TLS RpcException throw on a remote.
    /// </summary>
    public class GrpcException : Exception
    {
        private string? _stackTrace;

        public GrpcException(RpcException ex) : base($"{ex.StatusCode} {ex.Message}", ex)
        {
            Google.Rpc.Status? rpcStatus = ex.GetRpcStatus();
            ErrorInfo? errorInfo = rpcStatus?.GetDetail<ErrorInfo>();
            if (errorInfo != null && errorInfo.Metadata.TryGetValue("StackTrace", out var value))
            {
                _stackTrace = value;
            }
        }

        public override string? StackTrace
        {
            get { return _stackTrace; }
        }
    }
}