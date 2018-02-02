using System;
using System.Runtime.Serialization;

namespace Grumpy.RipplesMQ.Core.Exceptions
{
    /// <inheritdoc />
    [Serializable]
    public sealed class MessageBrokerException : Exception
    {
        /// <inheritdoc />
        private MessageBrokerException(SerializationInfo info, StreamingContext context) : base(info, context) { } 

        /// <inheritdoc />
        public MessageBrokerException(string serverName) : base("Message Broker not found Exception")
        {
            Data.Add(nameof(serverName), serverName);
        }
    }
}