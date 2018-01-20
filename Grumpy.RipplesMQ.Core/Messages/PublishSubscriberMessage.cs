using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Core.Messages
{
    public class PublishSubscriberMessage
    {
        public string SubscriberName { get; set; }
        public PublishMessage Message { get; set; }
    }
}