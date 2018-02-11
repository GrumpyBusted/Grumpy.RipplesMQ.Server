using System.Data.Entity;
using Grumpy.Entity.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;
using Microsoft.Extensions.Logging;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    /// <inheritdoc />
    public class Repositories : IRepositories
    {
        private readonly Entities _entities;
        private bool _disposed;
        private DbContextTransaction _dbContextTransaction;

        /// <inheritdoc />
        public Repositories(ILogger logger, IEntityConnectionConfig entityConnectionConfig)
        {
            _entities = new Entities(logger, entityConnectionConfig);
        }

        /// <inheritdoc />
        public IMessageRepository MessageRepository()
        {
            return new MessageRepository(_entities);
        }

        /// <inheritdoc />
        public IMessageBrokerServiceRepository MessageBrokerServiceRepository()
        {
            return new MessageBrokerServiceRepository(_entities);
        }

        /// <inheritdoc />
        public IMessageStateRepository MessageStateRepository()
        {
            return new MessageStateRepository(_entities);
        }

        /// <inheritdoc />
        public ISubscriberRepository SubscriberRepository()
        {
            return new SubscriberRepository(_entities);
        }

        /// <inheritdoc />
        public void Save()
        {
            _entities.SaveChanges();
        }

        /// <inheritdoc />
        public void BeginTransaction()
        {
            _dbContextTransaction = _entities.Database.BeginTransaction();
        }

        /// <inheritdoc />
        public void CommitTransaction()
        {
            _dbContextTransaction?.Commit();
        }
        
        /// <inheritdoc />
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

                if (disposing)
                {
                    _dbContextTransaction?.Dispose();
                    _entities.Dispose();
                }
            }
        }
    }
}