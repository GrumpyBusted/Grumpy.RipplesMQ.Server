using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    /// <inheritdoc />
    public class NullRepositoryContext : IRepositoryContext
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IMessageBrokerServiceRepository _messageBrokerServiceRepository;
        private readonly IMessageStateRepository _messageStateRepository;
        private readonly ISubscriberRepository _subscriberRepository;

        /// <inheritdoc />
        public NullRepositoryContext()
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
        public IMessageRepository MessageRepository => _messageRepository;

        /// <inheritdoc />
        public IMessageBrokerServiceRepository MessageBrokerServiceRepository => _messageBrokerServiceRepository;

        /// <inheritdoc />
        public IMessageStateRepository MessageStateRepository => _messageStateRepository;

        /// <inheritdoc />
        public ISubscriberRepository SubscriberRepository => _subscriberRepository;

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