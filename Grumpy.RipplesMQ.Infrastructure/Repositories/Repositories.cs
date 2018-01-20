using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    public class Repositories : IRepositories
    {
        private bool _disposed;

        public IMessageRepository MessageRepository()
        {
            return new MessageRepository();
        }

        public IMessageBrokerServiceRepository MessageBrokerServiceRepository()
        {
            return new MessageBrokerServiceRepository();
        }

        public IMessageStateRepository MessageStateRepository()
        {
            return new MessageStateRepository();
        }

        public ISubscriberRepository SubscriberRepository()
        {
            return new SubscriberRepository();
        }

        public void Save()
        {
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
            }
        }
    }
}