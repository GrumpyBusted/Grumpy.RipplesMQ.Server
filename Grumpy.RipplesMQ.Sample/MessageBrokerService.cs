using System.Configuration;
using System.Threading;
using Grumpy.Common.Extensions;
using Grumpy.RipplesMQ.Core.Interfaces;
using Grumpy.RipplesMQ.Server;
using Grumpy.ServiceBase;

namespace Grumpy.RipplesMQ.Sample
{
    public class MessageBrokerService : TopshelfServiceBase
    {
        private IMessageBroker _messageBroker;

        protected override void Process(CancellationToken cancellationToken)
        {
            var appSettings = ConfigurationManager.AppSettings;

            var messageBrokerBuilder = new MessageBrokerBuilder().WithServiceName(ServiceName);

            if (!appSettings["DatabaseServer"].NullOrEmpty())
                messageBrokerBuilder = messageBrokerBuilder.WithRepository(appSettings["DatabaseServer"], appSettings["DatabaseNAme"]);

            _messageBroker = messageBrokerBuilder.Build();

            _messageBroker.Start(cancellationToken);
        }

        protected override void Clean()
        {
            _messageBroker?.Dispose();
        }
    }
}