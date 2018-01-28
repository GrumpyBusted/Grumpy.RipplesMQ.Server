namespace Grumpy.RipplesMQ.Core
{
    /// <summary>
    /// Message Broker Configuration
    /// </summary>
    public class MessageBrokerConfig
    {
        /// <summary>
        /// Service Name
        /// </summary>
        public string ServiceName { get; set; }
        
        /// <summary>
        /// Remote Queue Name 
        /// </summary>
        public string RemoteQueueName { get; set; }
    }
}