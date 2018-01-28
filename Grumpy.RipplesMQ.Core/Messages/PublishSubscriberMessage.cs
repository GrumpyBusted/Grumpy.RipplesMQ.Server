using Grumpy.RipplesMQ.Shared.Messages;

namespace Grumpy.RipplesMQ.Core.Messages
{
    /// <summary>
    /// Publish/Subscribe Message
    /// </summary>
    public class PublishSubscriberMessage
    {
        /// <summary>
        /// Subscriber Name
        /// </summary>
        public string SubscriberName { get; set; }

        /// <summary>
        /// Publish Message
        /// </summary>
        public PublishMessage Message { get; set; }
    }
}