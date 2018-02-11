using System;
using System.Runtime.Serialization;
using Grumpy.Json;
using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Core.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    /// Exception sending publish text
    /// </summary>
    [Serializable]
    public sealed class PublishMessageException : Exception
    {
        private PublishMessageException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        public PublishMessageException(string text, SubscribeHandlerErrorMessage message) : base(text)
        {
            Data.Add(nameof(message), message.TrySerializeToJson());
        }
    }
}