﻿using Grumpy.Common;
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

        /// <inheritdoc />
        public MessageBrokerBuilder()
        {
            _processInformation = new ProcessInformation();
            _serviceName = _processInformation.ProcessName;
            _remoteQueueName = _serviceName.Replace("$", ".") + ".Remote";
            _repositoriesFactory = new NullRepositoriesFactory();
        }

        /// <summary>
        /// Set Service Name
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public MessageBrokerBuilder WithServiceName(string serviceName)
        {
            _serviceName = serviceName;

            return this;
        }

        /// <summary>
        /// Set Remote Queue Name
        /// </summary>
        /// <param name="remoteQueueName">Remote Queue Name</param>
        /// <returns></returns>
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
        /// <returns></returns>
        public MessageBrokerBuilder WithRepository(string databaseServer, string databaseName = "RipplesMQ")
        {
            if (!databaseServer.NullOrEmpty())
                _repositoriesFactory = new RepositoriesFactory(new EntityConnectionConfig(new DatabaseConnectionConfig(databaseServer, databaseName)));

            return this;
        }

        /// <summary>
        /// Build Message Broker
        /// </summary>
        /// <returns></returns>
        public IMessageBroker Build()
        {
            var messageBrokerConfig = new MessageBrokerConfig
            {
                ServiceName = _serviceName,
                RemoteQueueName = _remoteQueueName
            };

            var queueFactory = new QueueFactory();
            var queueHandlerFactory = new QueueHandlerFactory(queueFactory);

            return new MessageBroker(messageBrokerConfig, _repositoriesFactory, queueHandlerFactory, queueFactory, _processInformation);
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