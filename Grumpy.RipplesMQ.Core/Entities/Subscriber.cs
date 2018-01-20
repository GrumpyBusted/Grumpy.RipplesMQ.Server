using System;
using System.Diagnostics.CodeAnalysis;

namespace Grumpy.RipplesMQ.Core.Entities
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class Subscriber
    {
        public string Name { get; set; }
        public string Topic { get; set; }
        public string ServerName { get; set; }
        public string ServiceName { get; set; }
        public string InstanceName { get; set; }
        public string QueueName { get; set; }
        public DateTimeOffset LastRegisterDateTime { get; set; }
    }
}