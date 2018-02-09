using System;
using Grumpy.MessageQueue.Interfaces;

namespace Grumpy.RipplesMQ.Core.Dto
{
    /// <summary>
    /// Subscriber Handler
    /// </summary>
    public class SubscribeHandler
    {
        /// <summary>
        /// Message Topic
        /// </summary>
        public string Topic { get; set; }
        
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Message Type
        /// </summary>
        public string MessageType { get; set; }
        
        /// <summary>
        /// Server Name
        /// </summary>
        public string ServerName { get; set; }
        
        /// <summary>
        /// Queue Name
        /// </summary>
        public string QueueName { get; set; }
        
        /// <summary>
        /// Durable Subscriber
        /// </summary>
        public bool Durable { get; set; }
        
        /// <summary>
        /// Handshake Timestamp
        /// </summary>
        public DateTimeOffset? HandshakeDateTime { get; set; }
        
        /// <summary>
        /// Queue
        /// </summary>
        public IQueue Queue { get; set; }
    }
}