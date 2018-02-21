using System;
using System.Linq;
using FluentAssertions;
using Grumpy.Common;
using Grumpy.Entity;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;
using Grumpy.RipplesMQ.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Grumpy.RipplesMQ.Infrastructure.IntegrationTests
{
    public class MessageBrokerServiceRepositoryTests
    {
        private readonly EntityConnectionConfig _entityConnectionConfig;
        private const string ServerName = "MyServer";
        private readonly IRepositoryContextFactory _repositoryContextFactory;
        private readonly string _serviceName;

        public MessageBrokerServiceRepositoryTests()
        {
            _serviceName = UniqueKeyUtility.Generate();
            _entityConnectionConfig = new EntityConnectionConfig(new DatabaseConnectionConfig(@"(localdb)\MSSQLLocalDB", "Grumpy.RipplesMQ.Database_Model"));
            _repositoryContextFactory = new RepositoryContextFactory(NullLogger.Instance, _entityConnectionConfig);
        }

        [Fact]
        public void CanUseMessageBrokerServiceRepository()
        {
            try
            {
                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageBrokerServiceRepository;

                    cut.Get(ServerName, _serviceName).Should().BeNull();
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageBrokerServiceRepository;

                    cut.Insert(new MessageBrokerService
                    {
                        ServerName = ServerName,
                        ServiceName = _serviceName,
                        LocaleQueueName = "MyLocaleQueueName",
                        RemoteQueueName = "MyRemoteQueueName",
                        StartDateTime = DateTimeOffset.Now,
                        PulseDateTime = DateTimeOffset.Now
                    });

                    repositoryContext.Save();
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageBrokerServiceRepository;

                    var entity = cut.Get(ServerName, _serviceName);

                    entity.Should().NotBeNull();
                    entity.ServiceName.Should().Be(_serviceName);
                    entity.LocaleQueueName.Should().Be("MyLocaleQueueName");
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageBrokerServiceRepository;

                    var entity = cut.Get(ServerName, _serviceName);

                    entity.LocaleQueueName = "AnotherLocaleQueueName";
                    entity.RemoteQueueName = "MyRemoteQueueName";
                    entity.StartDateTime = DateTimeOffset.Now;
                    entity.PulseDateTime = DateTimeOffset.Now;

                    repositoryContext.Save();
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageBrokerServiceRepository;

                    var entity = cut.Get(ServerName, _serviceName);

                    entity.LocaleQueueName.Should().Be("AnotherLocaleQueueName");

                    cut.GetAll().Count().Should().BeGreaterOrEqualTo(1);
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageBrokerServiceRepository;

                    cut.Delete(ServerName, _serviceName);

                    repositoryContext.Save();
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageBrokerServiceRepository;

                    cut.Get(ServerName, _serviceName).Should().BeNull();
                }
            }
            finally
            {
                using (var entities = new Entities(_entityConnectionConfig))
                {
                    entities.MessageBrokerService.RemoveRange(entities.MessageBrokerService.Where(e => e.ServiceName == _serviceName));
                    entities.SaveChanges();
                }
            }
        }
    }
}
