using System.Configuration;
using System.Threading;
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

            var messageBrokerServiceConfig = new MessageBrokerServiceConfig
            {
                ServiceName = ServiceName,
                InstanceName = InstanceName,
                DatabaseServer = appSettings["DatabaseServer"],
                DatabaseName = appSettings["DatabaseName"]
            };
 
            _messageBroker = MessageBrokerBuilder.Build(messageBrokerServiceConfig);

            _messageBroker.Start(cancellationToken);
        }

        protected override void Clean()
        {
            _messageBroker?.Dispose();
        }
    }
}