using System;
using System.Diagnostics.CodeAnalysis;

namespace Grumpy.RipplesMQ.Core.Entities
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class Message
    {
        public string Id { get; set; }
        public string Topic { get; set; }
        public string Type { get; set; }
        public string Body { get; set; }
        public DateTimeOffset PublishDateTime { get; set; }
    }
}