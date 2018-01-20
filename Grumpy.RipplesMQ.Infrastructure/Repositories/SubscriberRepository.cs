using System;
using System.Linq;
using Grumpy.Json;
using Grumpy.RipplesMQ.Core.Entities;
using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    internal class SubscriberRepository : ISubscriberRepository
    {
        public void Insert(Subscriber subscriber)
        {
            Console.WriteLine($"Insert in SubscriberRepository {subscriber.SerializeToJson(false)}");
        }

        public void Update(Subscriber subscriber)
        {
            Console.WriteLine($"Update in SubscriberRepository {subscriber.SerializeToJson(false)}");
        }

        public Subscriber Get(string serverName, string queueName)
        {
            return GetAll().SingleOrDefault(e => e.ServerName == serverName && e.QueueName == queueName);
        }

        public IQueryable<Subscriber> GetAll()
        {
            return Enumerable.Empty<Subscriber>().AsQueryable();
        }
    }
}