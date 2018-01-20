namespace Grumpy.RipplesMQ.Core.Dto
{
    public class MessageBrokerServiceInformation
    {
        public string Id { get; set; }
        public string ServerName { get; set; }
        public string ServiceName { get; set; }
        public string InstanceName { get; set; }
        public string LocaleQueueName { get; set; }
        public string RemoteQueueName { get; set; }
    }
}