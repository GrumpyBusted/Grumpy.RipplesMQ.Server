namespace Grumpy.RipplesMQ.Core
{
    public class MessageBrokerConfig
    {
        public string ServiceName { get; set; }
        public string InstanceName { get; set; }
        public string RemoteQueueName { get; set; }
    }
}