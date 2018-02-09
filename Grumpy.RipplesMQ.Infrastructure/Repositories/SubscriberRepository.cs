using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    internal class SubscriberRepository : ISubscriberRepository
    {
        private readonly Entities _entities;

        public SubscriberRepository(Entities entities)
        {
            _entities = entities;
        }

        public void Insert(Subscriber subscriber)
        {
            _entities.Subscriber.Add(subscriber);
        }

        public Subscriber Get(string serverName, string queueName)
        {
            return _entities.Subscriber.SingleOrDefault(e => e.ServerName == serverName && e.QueueName == queueName);
        }

        public IEnumerable<Subscriber> GetAll()
        {
            return _entities.Subscriber;
        }

        public void Delete(string serverName, string queueName)
        {
            _entities.Subscriber.Remove(Get(serverName, queueName));
        }
    }
}