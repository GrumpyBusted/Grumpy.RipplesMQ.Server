using System;
using System.Collections.Generic;
using System.Threading;
using Grumpy.RipplesMQ.Core.Dto;

namespace Grumpy.RipplesMQ.Core.Interfaces
{
    public interface IMessageBroker : IDisposable
    {
        void Start(CancellationToken cancellationToken);
        void Handler(object message, CancellationToken cancellationToken);
        List<MessageBrokerService> MessageBrokerServices { get; }
        List<SubscribeHandler> SubscribeHandlers { get; }
        List<RequestHandler> RequestHandlers { get; }
    }
}