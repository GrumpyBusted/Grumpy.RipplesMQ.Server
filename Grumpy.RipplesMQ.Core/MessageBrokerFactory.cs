using Grumpy.Common.Interfaces;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Core.Interfaces;

namespace Grumpy.RipplesMQ.Core
{
    public class MessageBrokerFactory : IMessageBrokerFactory
    {
        private readonly MessageBrokerServiceConfig _messageBrokerServiceConfig;
        private readonly IRepositoriesFactory _repositoriesFactory;
        private readonly IQueueHandlerFactory _queueHandlerFactory;
        private readonly IQueueFactory _queueFactory;
        private readonly IProcessInformation _processInformation;

        public MessageBrokerFactory(MessageBrokerServiceConfig messageBrokerServiceConfig, IRepositoriesFactory repositoriesFactory, IQueueHandlerFactory queueHandlerFactory, IQueueFactory queueFactory, IProcessInformation processInformation)
        {
            _messageBrokerServiceConfig = messageBrokerServiceConfig;
            _repositoriesFactory = repositoriesFactory;
            _queueHandlerFactory = queueHandlerFactory;
            _queueFactory = queueFactory;
            _processInformation = processInformation;
        }

        public IMessageBroker Create()
        {
            return new MessageBroker(_messageBrokerServiceConfig, _repositoriesFactory, _queueHandlerFactory, _queueFactory, _processInformation);
        }
    }
}