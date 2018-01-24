using System.Threading;
using FluentAssertions;
using Grumpy.RipplesMQ.Core;
using Grumpy.RipplesMQ.Core.Interfaces;
using Xunit;

namespace Grumpy.RipplesMQ.Server.UnitTests
{
    public class MessageBrokerServiceTests
    {
        [Fact]
        public void MessageBrokerServiceCanStartAndStop()
        {
            var config = new MessageBrokerServiceConfig
            {
                ServiceName = "MyRipplesMQService",
                InstanceName = "1"
            };

            var cut = MessageBrokerBuilder.Build(config);;
            
            cut.Start(CancellationToken.None);
            Thread.Sleep(1000);
//            cut.Stop();
        }

        [Fact]
        public void CanBuildMessageBroker()
        {
            var cut = MessageBrokerBuilder.Build(null);

            cut.GetType().Should().Be(typeof(MessageBroker));
        }
    }
}