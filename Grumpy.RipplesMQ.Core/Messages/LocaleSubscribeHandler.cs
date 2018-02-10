namespace Grumpy.RipplesMQ.Core.Messages
{
    /// <summary>
    /// Locale Subscribe Handler
    /// </summary>
    public class LocaleSubscribeHandler
    {
        /// <summary>
        /// Message Topic
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// Message Type
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Subscribe Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Service Name
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Queue Name
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Durable Queue
        /// </summary>
        public bool Durable { get; set; }
    }
}