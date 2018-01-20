using System;
using System.Diagnostics.CodeAnalysis;

namespace Grumpy.RipplesMQ.Core.Entities
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class MessageState
    {
        public string MessageId { get; set; }
        public string SubscriberName { get; set; }
        public string State { get; set; }
        public int ErrorCount { get; set; }
        public DateTimeOffset UpdateDateTime { get; set; }
    }
}