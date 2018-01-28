using System.Threading;
using FluentAssertions;
using Grumpy.RipplesMQ.Core;
using Xunit;

namespace Grumpy.RipplesMQ.Server.UnitTests
{
    public class MessageBrokerServiceTests
    {
        [Fact]
        public void CanBuildMessageBroker()
        {
            var cut = (MessageBroker)new MessageBrokerBuilder();

            cut.GetType().Should().Be(typeof(MessageBroker));
        }

        [Fact]
        public void CanBuildMessageBrokerUsingConfig()
        {
            new MessageBrokerBuilder().WithServiceName("MyRipplesMQService").WithRemoteQueueName("MyRipplesMQService.RemoteQueue").WithRepository(@"(localDB)/MSSQLLocalDB", "RipplesMQ_Database").Build();
        }

        [Fact]
        public void MessageBrokerCanStartAndStop()
        {
            var cut = new MessageBrokerBuilder().Build();
            
            cut.Start(CancellationToken.None);
            Thread.Sleep(1000);
            cut.Stop();
        }
    }
}