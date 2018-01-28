using System.Collections.Generic;

namespace Grumpy.RipplesMQ.Core.Messages
{
    /// <summary>
    /// Message Broker Handshake Message
    /// </summary>
    public class MessageBrokerHandshakeMessage
    {
        /// <summary>
        /// Message Broker id
        /// </summary>
        public string MessageBrokerId { get; set; }
        
        /// <summary>
        /// Server Name
        /// </summary>
        public string ServerName { get; set; }
        
        /// <summary>
        /// Queue Name
        /// </summary>
        public string QueueName { get; set; }
        
        /// <summary>
        /// List of Locale Subscribe Handlers
        /// </summary>
        public ICollection<LocaleSubscribeHandler> LocaleSubscribeHandlers { get; set; }
        
        /// <summary>
        /// List of Locale Request Handlers
        /// </summary>
        public ICollection<LocaleRequestHandler> LocaleRequestHandlers { get; set; }
    }
}