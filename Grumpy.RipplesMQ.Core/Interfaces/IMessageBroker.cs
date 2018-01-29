using System;
using System.Collections.Generic;
using System.Threading;
using Grumpy.RipplesMQ.Core.Dto;

namespace Grumpy.RipplesMQ.Core.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// Message Broker Server
    /// </summary>
    public interface IMessageBroker : IDisposable
    {
        /// <summary>
        /// Start Message Broker
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        void Start(CancellationToken cancellationToken);

        /// <summary>
        /// Stop
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Message handler message
        /// </summary>
        /// <param name="message">The Message</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        void Handler(object message, CancellationToken cancellationToken);

        /// <summary>
        /// Error message handler message
        /// </summary>
        /// <param name="message">The Message</param>
        /// <param name="exception">Exception</param>
        void ErrorHandler(object message, Exception exception);

        /// <summary>
        /// List of Message Broker Services
        /// </summary>
        List<MessageBrokerService> MessageBrokerServices { get; }
        
        /// <summary>
        /// List of Subscriber handlers
        /// </summary>
        List<SubscribeHandler> SubscribeHandlers { get; }
        
        /// <summary>
        /// List of Request handlers
        /// </summary>
        List<RequestHandler> RequestHandlers { get; }
    }
}