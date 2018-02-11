using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Core.Exceptions
{
    /// <inheritdoc />
    [Serializable]
    public sealed class SubscribeHandlerNotFoundException : Exception
    {
        private SubscribeHandlerNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        public SubscribeHandlerNotFoundException(string subscriberName, PublishMessage message) : base("Subscribe Handler not Found Exception")
        {
            Data.Add(nameof(subscriberName), subscriberName);
            Data.Add(nameof(message), message.TrySerializeToJson());
        }
    }
}