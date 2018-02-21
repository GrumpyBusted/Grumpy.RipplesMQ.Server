using System;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    /// <inheritdoc />
    /// <summary>
    /// Repositories for Message Broker
    /// </summary>
    public interface IRepositoryContext : IDisposable
    {
        /// <summary>
        /// Message Repository
        /// </summary>
        /// <returns>Message Repository</returns>
        IMessageRepository MessageRepository { get; }

        /// <summary>
        /// Message Broker Service Repository
        /// </summary>
        /// <returns>Message Broker Service Repository</returns>
        IMessageBrokerServiceRepository MessageBrokerServiceRepository { get; }

        /// <summary>
        /// Message/Subscriber State Repository
        /// </summary>
        /// <returns>Message/Subscriber State Repository</returns>
        IMessageStateRepository MessageStateRepository { get; }

        /// <summary>
        /// Subscriber Repository
        /// </summary>
        /// <returns>Subscriber Repository</returns>
        ISubscriberRepository SubscriberRepository { get; }

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