namespace Grumpy.RipplesMQ.Core.Messages
{
    public class LocaleSubscribeHandler
    {
        public string Topic { get; set; }
        public string Name { get; set; }
        public string QueueName { get; set; }
        public bool Durable { get; set; }
    }
}