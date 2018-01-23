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

        public void Insert(Core.Entities.Subscriber subscriber)
        {
            _entities.Subscriber.Add(new Subscriber
            {
                ServerName = subscriber.ServerName,
                ServiceName = subscriber.ServiceName,
                InstanceName = subscriber.InstanceName,
                Name = subscriber.Name,
                Topic = subscriber.Topic,
                QueueName = subscriber.QueueName,
                LastRegisterDateTime = subscriber.LastRegisterDateTime
            });
        }

        public void Update(Core.Entities.Subscriber subscriber)
        {
            var entity = GetEntity(subscriber.ServerName, subscriber.QueueName);

            entity.ServiceName = subscriber.ServiceName;
            entity.InstanceName = subscriber.InstanceName;
            entity.Name = subscriber.Name;
            entity.Topic = subscriber.Topic;
            entity.LastRegisterDateTime = subscriber.LastRegisterDateTime;
        }

        public Core.Entities.Subscriber Get(string serverName, string queueName)
        {
            return FromEntity(GetEntity(serverName, queueName));  
        }

        private Subscriber GetEntity(string serverName, string queueName)
        {
            return _entities.Subscriber.SingleOrDefault(e => e.ServerName == serverName && e.QueueName == queueName);
        }

        public IEnumerable<Core.Entities.Subscriber> GetAll()
        {
            return _entities.Subscriber.ToList().Select(FromEntity);
        }

        private static Core.Entities.Subscriber FromEntity(Subscriber subscriber)
        {
            return subscriber == null ? null : new Core.Entities.Subscriber
            {
                ServerName = subscriber.ServerName,
                ServiceName = subscriber.ServiceName,
                InstanceName = subscriber.InstanceName,
                Name = subscriber.Name,
                Topic = subscriber.Topic,
                QueueName = subscriber.QueueName,
                LastRegisterDateTime = subscriber.LastRegisterDateTime
            };
        }
    }
}