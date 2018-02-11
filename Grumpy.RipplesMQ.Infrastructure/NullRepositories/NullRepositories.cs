using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    /// <inheritdoc />
    public class NullRepositories : IRepositories
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IMessageBrokerServiceRepository _messageBrokerServiceRepository;
        private readonly IMessageStateRepository _messageStateRepository;
        private readonly ISubscriberRepository _subscriberRepository;

        /// <inheritdoc />
        public NullRepositories()
        {
            _messageRepository = new NullMessageRepository();
            _messageBrokerServiceRepository = new NullMessageBrokerServiceRepository();
            _messageStateRepository = new NullMessageStateRepository();
            _subscriberRepository = new NullSubscriberRepository();
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public IMessageRepository MessageRepository()
        {
            return _messageRepository;
        }

        /// <inheritdoc />
        public IMessageBrokerServiceRepository MessageBrokerServiceRepository()
        {
            return _messageBrokerServiceRepository;
        }

        /// <inheritdoc />
        public IMessageStateRepository MessageStateRepository()
        {
            return _messageStateRepository;
        }

        /// <inheritdoc />
        public ISubscriberRepository SubscriberRepository()
        {
            return _subscriberRepository;
        }

        /// <inheritdoc />
        public void Save()
        {
        }

        /// <inheritdoc />
        public void BeginTransaction()
        {
        }

        /// <inheritdoc />
        public void CommitTransaction()
        {
        }
    }
}