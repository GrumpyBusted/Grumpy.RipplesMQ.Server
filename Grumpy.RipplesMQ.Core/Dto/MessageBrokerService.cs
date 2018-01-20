using System;
using Grumpy.MessageQueue.Interfaces;

namespace Grumpy.RipplesMQ.Core.Dto
{
    public class MessageBrokerService
    {
        public string Id { get; set; }
        public string ServerName { get; set; }
        public string RemoteQueueName { get; set; }
        public DateTimeOffset? LastHandshakeDateTime { get; set; }
        public IQueue Queue { get; set; }
    }
}