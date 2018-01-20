using System;
using Grumpy.MessageQueue.Interfaces;

namespace Grumpy.RipplesMQ.Core.Dto
{
    public class RequestHandler
    {
        public string Name { get; set; }
        public string ServerName { get; set; }
        public string QueueName { get; set; }
        public DateTimeOffset? LastHandshakeDateTime { get; set; }
        public IQueue Queue { get; set; }
    }
}