using System.Threading;
using Grumpy.RipplesMQ.Core.Interfaces;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Server.UnitTests
{
    public class MessageBrokerServiceTests
    {
        [Fact]
        public void MessageBrokerServiceCanStartAndStop()
        {
            var messageBroker = Substitute.For<IMessageBroker>();
            var messageBrokerFactory = Substitute.For<IMessageBrokerFactory>();
            messageBrokerFactory.Create().Returns(messageBroker);

            var cut = new MessageBrokerService(messageBrokerFactory);
            
            cut.Start();
            Thread.Sleep(1000);
            cut.Stop();
            
            messageBrokerFactory.Received(1).Create();
            messageBroker.Received(1).Dispose();
        }
    }
}