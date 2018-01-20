using FluentAssertions;
using Grumpy.Common.Interfaces;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Core.UnitTests
{
    public class MessageBrokerFactoryTests
    {
        [Fact]
        public void MessageBrokerFactoryCanCreateInstance()
        {
            var messageBrokerFactory =  new MessageBrokerFactory(new MessageBrokerServiceConfig(), Substitute.For<IRepositoriesFactory>(), Substitute.For<IQueueHandlerFactory>(), Substitute.For<IQueueFactory>(), Substitute.For<IProcessInformation>());
           
            messageBrokerFactory.Create().Should().NotBeNull();
        }
    }
}