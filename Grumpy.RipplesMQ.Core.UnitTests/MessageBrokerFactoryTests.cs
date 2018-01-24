using FluentAssertions;
using Grumpy.Common.Interfaces;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Core.Interfaces;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Core.UnitTests
{
    public class MessageBrokerFactoryTests
    {
        [Fact]
        public void MessageBrokerFactoryCanCreateInstance()
        {
            var messageBrokerFactory = (IMessageBrokerFactory)new MessageBrokerFactory(new MessageBrokerConfig(), Substitute.For<IRepositoriesFactory>(), Substitute.For<IQueueHandlerFactory>(), Substitute.For<IQueueFactory>(), Substitute.For<IProcessInformation>());
           
            messageBrokerFactory.Create().Should().NotBeNull();
        }
    }
}