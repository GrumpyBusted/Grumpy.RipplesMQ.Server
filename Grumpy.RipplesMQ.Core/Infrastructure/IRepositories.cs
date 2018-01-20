using System;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    public interface IRepositories : IDisposable
    {
        IMessageRepository MessageRepository();
        IMessageBrokerServiceRepository MessageBrokerServiceRepository();
        IMessageStateRepository MessageStateRepository();
        ISubscriberRepository SubscriberRepository();
        void Save();
    }
}