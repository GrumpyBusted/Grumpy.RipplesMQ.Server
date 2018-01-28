using System;
using Grumpy.MessageQueue.Interfaces;

namespace Grumpy.RipplesMQ.Core.Dto
{
    /// <summary>
    /// Message Broker Service
    /// </summary>
    public class MessageBrokerService
    {
        /// <summary>
        /// Message Broker Service Id
        /// </summary>
        public string Id { get; set; }
 
        /// <summary>
        /// Server Name
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Remote Queue Name
        /// </summary>
        public string RemoteQueueName { get; set; }
        
        /// <summary>
        /// Handshake Timestamp
        /// </summary>
        public DateTimeOffset? HandshakeDateTime { get; set; }
        
        /// <summary>
        /// Queue
        /// </summary>
        public IQueue Queue { get; set; }

        /// <summary>
        /// Error Count - Number of times unable to send handshake
        /// </summary>
        public int ErrorCount { get; set; }
    }
}