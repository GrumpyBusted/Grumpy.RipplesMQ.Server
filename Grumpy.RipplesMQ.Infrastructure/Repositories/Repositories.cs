using Grumpy.Entity.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    public class Repositories : IRepositories
    {
        private readonly Entities _entities;
        private bool _disposed;

        public Repositories(IEntityConnectionConfig entityConnectionConfig)
        {
            _entities = new Entities(entityConnectionConfig);
        }

        public IMessageRepository MessageRepository()
        {
            return new MessageRepository(_entities);
        }

        public IMessageBrokerServiceRepository MessageBrokerServiceRepository()
        {
            return new MessageBrokerServiceRepository(_entities);
        }

        public IMessageStateRepository MessageStateRepository()
        {
            return new MessageStateRepository(_entities);
        }

        public ISubscriberRepository SubscriberRepository()
        {
            return new SubscriberRepository(_entities);
        }

        public void Save()
        {
            _entities.SaveChanges();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        // ReSharper disable once UnusedParameter.Local
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                _entities.Dispose();
            }
        }
    }
}