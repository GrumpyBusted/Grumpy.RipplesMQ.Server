using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    /// <inheritdoc />
    public class NullMessageBrokerServiceRepository : IMessageBrokerServiceRepository
    {
        /// <inheritdoc />
        public void Insert(MessageBrokerService messageBroker)
        {
        }

        /// <inheritdoc />
        public MessageBrokerService Get(string serverName, string serviceName)
        {
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<MessageBrokerService> GetAll()
        {
            return Enumerable.Empty<MessageBrokerService>();
        }

        /// <inheritdoc />
        public void Delete(string serverName, string serviceName)
        {
        }
    }
}