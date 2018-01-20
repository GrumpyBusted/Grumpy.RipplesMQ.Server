using System.Collections.Generic;

namespace Grumpy.RipplesMQ.Core.Messages
{
    public class MessageBrokerHandshakeMessage
    {
        public string MessageBrokerId { get; set; }
        public string ServerName { get; set; }
        public string QueueName { get; set; }
        public ICollection<LocaleSubscribeHandler> LocaleSubscribeHandlers { get; set; }
        public ICollection<LocaleRequestHandler> LocaleRequestHandlers { get; set; }
    }
}