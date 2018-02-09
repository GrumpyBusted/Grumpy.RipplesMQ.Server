namespace Grumpy.RipplesMQ.Core.Messages
{
    /// <summary>
    /// Locale Request Handler
    /// </summary>
    public class LocaleRequestHandler
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Message Type
        /// </summary>
        public string RequestType { get; set; }

        /// <summary>
        /// Message Type
        /// </summary>
        public string ResponseType { get; set; }

        /// <summary>
        /// Queue name
        /// </summary>
        public string QueueName { get; set; }
    }
}