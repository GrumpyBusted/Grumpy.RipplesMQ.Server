using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    internal class MessageBrokerServiceRepository : IMessageBrokerServiceRepository
    {
        private readonly Entities _entities;

        public MessageBrokerServiceRepository(Entities entities)
        {
            _entities = entities;
        }

        public void Insert(Core.Entities.MessageBrokerService messageBrokerService)
        {
            _entities.MessageBrokerService.Add(new MessageBrokerService
            {
                ServerName = messageBrokerService.ServerName,
                ServiceName = messageBrokerService.ServiceName,
                InstanceName = messageBrokerService.InstanceName,
                RemoteQueueName = messageBrokerService.RemoteQueueName,
                LocaleQueueName = messageBrokerService.LocaleQueueName,
                LastStartDateTime = messageBrokerService.LastStartDateTime
            });
        }

        public void Update(Core.Entities.MessageBrokerService messageBrokerService)
        {
            var entity = GetEntity(messageBrokerService.ServerName, messageBrokerService.ServiceName, messageBrokerService.InstanceName);

            entity.RemoteQueueName = messageBrokerService.RemoteQueueName;
            entity.LocaleQueueName = messageBrokerService.LocaleQueueName;
            entity.LastStartDateTime = messageBrokerService.LastStartDateTime;
        }

        public Core.Entities.MessageBrokerService Get(string serverName, string serviceName, string instanceName)
        {
            return FromEntity(GetEntity(serverName, serviceName, instanceName));
        }

        public IEnumerable<Core.Entities.MessageBrokerService> GetAll()
        {
            return _entities.MessageBrokerService.ToList().Select(FromEntity);
        }
        
        private static Core.Entities.MessageBrokerService FromEntity(MessageBrokerService messageBrokerService)
        {
            return messageBrokerService == null ? null : new Core.Entities.MessageBrokerService
            {
                ServerName = messageBrokerService.ServerName,
                ServiceName = messageBrokerService.ServiceName,
                InstanceName = messageBrokerService.InstanceName,
                RemoteQueueName = messageBrokerService.RemoteQueueName,
                LocaleQueueName = messageBrokerService.LocaleQueueName,
                LastStartDateTime = messageBrokerService.LastStartDateTime
            };
        }

        private MessageBrokerService GetEntity(string serverName, string serviceName, string instanceName)
        {
            return _entities.MessageBrokerService.SingleOrDefault(e => e.ServerName == serverName && e.ServiceName == serviceName && e.InstanceName == instanceName);
        }
    }
}