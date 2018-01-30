using FluentAssertions;
using Grumpy.Common.Interfaces;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Core.IntegrationTests
{
    public class MessageBrokerTests
    {
        [Fact]
        public void CanCreateInstance()
        {
            new MessageBroker(NullLogger.Instance, new MessageBrokerConfig(), Substitute.For<IRepositoriesFactory>(), Substitute.For<IQueueHandlerFactory>(), Substitute.For<IQueueFactory>(), Substitute.For<IProcessInformation>()).Should().NotBeNull();
        }
    }
}
