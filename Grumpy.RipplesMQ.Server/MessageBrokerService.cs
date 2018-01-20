using Grumpy.Common.ToBe;
using System.Threading;
using Grumpy.RipplesMQ.Core.Interfaces;

namespace Grumpy.RipplesMQ.Server
{
    public class MessageBrokerService : CancelableServiceBase, ITopshelfService
    {
        private readonly IMessageBrokerFactory _messageBrokerFactory;
        private IMessageBroker _messageBroker;

        public MessageBrokerService(IMessageBrokerFactory messageBrokerFactory)
        {
            _messageBrokerFactory = messageBrokerFactory;
        }

        protected override void Process(CancellationToken cancellationToken)
        {
            _messageBroker = _messageBrokerFactory.Create();

            _messageBroker.Start(cancellationToken);
        }

        protected override void Clean()
        {
            _messageBroker?.Dispose();
        }
    }
}