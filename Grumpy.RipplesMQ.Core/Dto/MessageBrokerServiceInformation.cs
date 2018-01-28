namespace Grumpy.RipplesMQ.Core.Dto
{
    /// <summary>
    /// Message Broker Service Information
    /// </summary>
    public class MessageBrokerServiceInformation
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Server Name
        /// </summary>
        public string ServerName { get; set; }
        
        /// <summary>
        /// Service Name
        /// </summary>
        public string ServiceName { get; set; }
        
        /// <summary>
        /// Locale Queue Name
        /// </summary>
        public string LocaleQueueName { get; set; }
        
        /// <summary>
        /// Remote Queue Name
        /// </summary>
        public string RemoteQueueName { get; set; }
    }
}