using System;
using System.Runtime.Serialization;
using Grumpy.RipplesMQ.Core.Dto;

namespace Grumpy.RipplesMQ.Core.Exceptions
{
    /// <inheritdoc />
    [Serializable]
    public sealed class MessageBrokerQueueException : Exception
    {
        private MessageBrokerQueueException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc />
        public MessageBrokerQueueException(MessageBrokerService messageBrokerService) : base("Error connecting to Message Broker Queue")
        {
            Data.Add(nameof(messageBrokerService), messageBrokerService);
        }
    }
}