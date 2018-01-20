using System;
using System.Linq;
using Grumpy.Json;
using Grumpy.RipplesMQ.Core.Entities;
using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    internal class MessageBrokerServiceRepository : IMessageBrokerServiceRepository
    {
        public void Insert(MessageBrokerService messageBrokerService)
        {
            Console.WriteLine($"Insert in MessageBrokerServiceRepository {messageBrokerService.SerializeToJson(false)}");
        }

        public void Update(MessageBrokerService messageBrokerService)
        {
            Console.WriteLine($"Update in MessageBrokerServiceRepository {messageBrokerService.SerializeToJson(false)}");
        }

        public MessageBrokerService Get(string serverName, string serviceName, string instanceName)
        {
            return  GetAll().SingleOrDefault(b => b.ServerName == serverName && b.ServiceName == serviceName && b.InstanceName == instanceName);
        }

        public IQueryable<MessageBrokerService> GetAll()
        {
            return Enumerable.Empty<MessageBrokerService>().AsQueryable();
        }
    }
}