using Grumpy.Common;
using Grumpy.Common.Extensions;
using Grumpy.Common.Interfaces;
using Grumpy.Entity;
using Grumpy.MessageQueue;
using Grumpy.MessageQueue.Msmq;
using Grumpy.RipplesMQ.Core;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Core.Interfaces;
using Grumpy.RipplesMQ.Infrastructure.NullRepositories;
using Grumpy.RipplesMQ.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Grumpy.RipplesMQ.Server
{
    /// <summary>
    /// Builder for Message Broker Server
    /// </summary>
    public class MessageBrokerBuilder
    {
        private readonly IProcessInformation _processInformation;
        private string _serviceName;
        private string _remoteQueueName;
        private IRepositoriesFactory _repositoriesFactory;
        private ILogger _logger;

        /// <inheritdoc />
        public MessageBrokerBuilder()
        {
            _logger = NullLogger.Instance;
            _processInformation = new ProcessInformation();
            _serviceName = _processInformation.ProcessName;
            _remoteQueueName = _serviceName.Replace("$", ".") + ".Remote";
            _repositoriesFactory = new NullRepositoriesFactory();
        }

        /// <summary>
        /// Set Service Name
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns>Builder for Message Broker Server</returns>
        public MessageBrokerBuilder WithServiceName(string serviceName)
        {
            _serviceName = serviceName;

            return this;
        }

        /// <summary>
        /// Set Logger
        /// </summary>
        /// <param name="logger"></param>
        /// <returns>Builder for Message Broker Server</returns>
        public MessageBrokerBuilder WithLogger(ILogger logger)
        {
            _logger = logger;

            return this;
        }

        /// <summary>
        /// Set Remote Queue Name
        /// </summary>
        /// <param name="remoteQueueName">Remote Queue Name</param>
        /// <returns>Builder for Message Broker Server</returns>
        public MessageBrokerBuilder WithRemoteQueueName(string remoteQueueName)
        {
            _remoteQueueName = remoteQueueName;

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="databaseServer"></param>
        /// <param name="databaseName"></param>
        /// <returns>Builder for Message Broker Server</returns>
        public MessageBrokerBuilder WithRepository(string databaseServer, string databaseName = "RipplesMQ")
        {
            if (!databaseServer.NullOrEmpty())
                _repositoriesFactory = new RepositoriesFactory(_logger, new EntityConnectionConfig(new DatabaseConnectionConfig(databaseServer, databaseName)));

            return this;
        }

        /// <summary>
        /// Build Message Broker
        /// </summary>
        /// <returns>The Message Bus</returns>
        public IMessageBroker Build()
        {
            var messageBrokerConfig = new MessageBrokerConfig
            {
                ServiceName = _serviceName,
                RemoteQueueName = _remoteQueueName
            };

            var queueFactory = new QueueFactory(_logger);
            var queueHandlerFactory = new QueueHandlerFactory(_logger, queueFactory);

            return new MessageBroker(_logger, messageBrokerConfig, _repositoriesFactory, queueHandlerFactory, queueFactory, _processInformation);
        }

        /// <summary>
        /// Build Message Broker
        /// </summary>
        /// <param name="messageBusBuilder"></param>
        /// <returns>The Message Bus</returns>
        public static implicit operator MessageBroker(MessageBrokerBuilder messageBusBuilder)
        {
            return (MessageBroker)messageBusBuilder.Build();
        }

    }
}