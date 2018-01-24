using Grumpy.Common;
using Grumpy.Common.Extensions;
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
    public static class MessageBrokerBuilder
    {
        public static IMessageBroker Build(MessageBrokerServiceConfig messageBrokerServiceConfig)
        {
            var messageBrokerConfig = new MessageBrokerConfig
            {
                ServiceName = messageBrokerServiceConfig?.ServiceName.NullOrEmpty() ?? true ? "RipplesMQ.MessageBroker" : messageBrokerServiceConfig.ServiceName,
                InstanceName = messageBrokerServiceConfig?.InstanceName.NullOrEmpty() ?? true ? "" : messageBrokerServiceConfig.InstanceName
            };

            messageBrokerConfig.RemoteQueueName = messageBrokerConfig.ServiceName + (messageBrokerConfig.InstanceName.NullOrEmpty() ? "" : $".{messageBrokerConfig.InstanceName}") + ".Remote";

            var databaseServer = messageBrokerServiceConfig?.DatabaseServer.NullOrEmpty() ?? true ? null : messageBrokerServiceConfig.DatabaseServer;
            var databaseName = messageBrokerServiceConfig?.DatabaseName.NullOrEmpty() ?? true ? "RipplesMQ" : messageBrokerServiceConfig.DatabaseName;

            var repositoriesFactory = databaseServer == null ? new NullRepositoriesFactory() : (IRepositoriesFactory)new RepositoriesFactory(new EntityConnectionConfig(new DatabaseConnectionConfig(databaseServer, databaseName)));
            var queueFactory = new QueueFactory();
            var processInformation = new ProcessInformation();
            var queueHandlerFactory = new QueueHandlerFactory(queueFactory);

            return new MessageBroker(messageBrokerConfig, repositoriesFactory, queueHandlerFactory, queueFactory, processInformation);
        }
    }
}