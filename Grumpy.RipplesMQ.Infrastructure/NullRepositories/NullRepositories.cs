using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    public class NullRepositories : IRepositories
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IMessageBrokerServiceRepository _messageBrokerServiceRepository;
        private readonly IMessageStateRepository _messageStateRepository;
        private readonly ISubscriberRepository _subscriberRepository;

        public NullRepositories()
        {
            _messageRepository = new NullMessageRepository();
            _messageBrokerServiceRepository = new NullMessageBrokerServiceRepository();
            _messageStateRepository = new NullMessageStateRepository();
            _subscriberRepository = new NullSubscriberRepository();
        }

        public void Dispose()
        {
        }

        public IMessageRepository MessageRepository()
        {
            return _messageRepository;
        }

        public IMessageBrokerServiceRepository MessageBrokerServiceRepository()
        {
            return _messageBrokerServiceRepository;
        }

        public IMessageStateRepository MessageStateRepository()
        {
            return _messageStateRepository;
        }

        public ISubscriberRepository SubscriberRepository()
        {
            return _subscriberRepository;
        }

        public void Save()
        {
        }
    }
}