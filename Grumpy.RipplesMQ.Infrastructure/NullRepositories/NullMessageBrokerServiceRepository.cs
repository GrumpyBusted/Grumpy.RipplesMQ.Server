using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Entities;
using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    public class NullMessageBrokerServiceRepository : IMessageBrokerServiceRepository
    {
        public void Insert(MessageBrokerService messageBroker)
        {
        }

        public void Update(MessageBrokerService messageBroker)
        {
        }

        public MessageBrokerService Get(string serverName, string serviceName, string instanceName)
        {
            return null;
        }

        public IEnumerable<MessageBrokerService> GetAll()
        {
            return Enumerable.Empty<MessageBrokerService>();
        }
    }
}