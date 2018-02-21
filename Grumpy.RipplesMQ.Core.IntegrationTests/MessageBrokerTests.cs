using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Grumpy.Common;
using Grumpy.Common.Interfaces;
using Grumpy.Entity;
using Grumpy.MessageQueue;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Infrastructure.Repositories;
using Grumpy.RipplesMQ.Shared.Messages;
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
            new MessageBroker(NullLogger.Instance, new MessageBrokerConfig(), Substitute.For<IRepositoryContextFactory>(), Substitute.For<IQueueHandlerFactory>(), Substitute.For<IQueueFactory>(), Substitute.For<IProcessInformation>()).Should().NotBeNull();
        }

        [Fact(Skip = "Add data to database")]
        public void CanStartMessageBroker()
        {
            var queueFactory = Substitute.For<IQueueFactory>();
            var messageBrokerConfig = new MessageBrokerConfig { RemoteQueueName = "MyRemoteQueueName", ServiceName = "TestBroker" };
            var entityConnectionConfig = new EntityConnectionConfig(new DatabaseConnectionConfig("(localdb)\\MSSQLLocalDB", "ABJA_Ripples"));
            var logger = NullLogger.Instance;
            var repositoryContextFactory = new RepositoryContextFactory(logger, entityConnectionConfig);
            var queueHandlerFactory = new QueueHandlerFactory(logger, queueFactory);
            var processInformation = new ProcessInformation();

            using (var messageBroker = new MessageBroker(logger, messageBrokerConfig, repositoryContextFactory, queueHandlerFactory, queueFactory, processInformation))
            {
                var cancellationToken = new CancellationToken();
                messageBroker.Start(cancellationToken);

                messageBroker.Handler(new MessageBusServiceHandshakeMessage
                {
                    ServiceName = "MyServiceName",
                    ServerName = "MyTestServer",
                    ReplyQueue = "MyQueue",
                    SendDateTime = DateTimeOffset.Now,
                    SubscribeHandlers = new List<SubscribeHandler> { new SubscribeHandler { Name = "MySubscriber", QueueName = "MyQueue", Durable = true, MessageType = "string", Topic = "MyTopic" } }
                });
            }
        }
    }
}
