using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Core.Exceptions
{
    /// <inheritdoc />
    [Serializable]
    public sealed class RequestHandlerNotFoundException : Exception
    {
        private RequestHandlerNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        public RequestHandlerNotFoundException(RequestMessage message) : base("Request Handler not Found Exception")
        {
            Data.Add(nameof(message), message.TrySerializeToJson());
        }
    }
}