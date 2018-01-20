using System;
using System.Diagnostics.CodeAnalysis;

namespace Grumpy.RipplesMQ.Core.Entities
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class MessageBrokerService
    {
        public string ServerName { get; set; }
        public string ServiceName { get; set; }
        public string InstanceName { get; set; }
        public string RemoteQueueName { get; set; }
        public string LocaleQueueName { get; set; }
        public DateTimeOffset LastStartDateTime { get; set; }
    }
}