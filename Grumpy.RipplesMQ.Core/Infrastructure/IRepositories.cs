using System;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    /// <inheritdoc />
    /// <summary>
    /// Repositories for Message Broker
    /// </summary>
    public interface IRepositories : IDisposable
    {
        /// <summary>
        /// Message Repository
        /// </summary>
        /// <returns>Message Repository</returns>
        IMessageRepository MessageRepository();
        
        /// <summary>
        /// Message Broker Service Repository
        /// </summary>
        /// <returns>Message Broker Service Repository</returns>
        IMessageBrokerServiceRepository MessageBrokerServiceRepository();
        
        /// <summary>
        /// Message/Subscriber State Repository
        /// </summary>
        /// <returns>Message/Subscriber State Repository</returns>
        IMessageStateRepository MessageStateRepository();

        /// <summary>
        /// Subscriber Repository
        /// </summary>
        /// <returns>Subscriber Repository</returns>
        ISubscriberRepository SubscriberRepository();

        /// <summary>
        /// Save changes
        /// </summary>
        void Save();

        /// <summary>
        /// Begin Database Transaction
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Commit Database Transaction
        /// </summary>
        void CommitTransaction();
    }
}