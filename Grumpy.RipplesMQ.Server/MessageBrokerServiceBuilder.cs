using Grumpy.Common;
using Grumpy.Common.Extensions;
using Grumpy.MessageQueue;
using Grumpy.MessageQueue.Msmq;
using Grumpy.RipplesMQ.Core;
using Grumpy.RipplesMQ.Infrastructure.Repositories;

namespace Grumpy.RipplesMQ.Server
{
    internal static class MessageBrokerServiceBuilder
    {
        public static MessageBrokerService Build(string serviceName)
        {
            var array = serviceName.Split('$');

            var messageBrokerServiceConfig = new MessageBrokerServiceConfig
            {
                ServiceName = array.Length == 2 ? array[0] : serviceName,
                InstanceName = array.Length == 2 ? array[1] : ""
            };

            messageBrokerServiceConfig.RemoteQueueName = messageBrokerServiceConfig.ServiceName + (messageBrokerServiceConfig.InstanceName.NullOrEmpty() ? "" : $".{messageBrokerServiceConfig.InstanceName}") + ".remote";

            var queueFactory = new QueueFactory();
            var processInformation = new ProcessInformation();
            var queueHandlerFactory = new QueueHandlerFactory(queueFactory);
            var repositoriesFactory = new RepositoriesFactory();
            var messageBrokerFactory = new MessageBrokerFactory(messageBrokerServiceConfig, repositoriesFactory, queueHandlerFactory, queueFactory, processInformation);

            return new MessageBrokerService(messageBrokerFactory);
        }
    }
}