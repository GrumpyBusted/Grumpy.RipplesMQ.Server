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

        public void Insert(MessageBrokerService messageBrokerService)
        {
            _entities.MessageBrokerService.Add(messageBrokerService);
        }

        public MessageBrokerService Get(string serverName, string serviceName)
        {
            return _entities.MessageBrokerService.SingleOrDefault(e => e.ServerName == serverName && e.ServiceName == serviceName);
        }

        public IEnumerable<MessageBrokerService> GetAll()
        {
            return _entities.MessageBrokerService;
        }

        public void Delete(string serverName, string serviceName)
        {
            _entities.MessageBrokerService.Remove(Get(serverName, serviceName));
        }
    }
}