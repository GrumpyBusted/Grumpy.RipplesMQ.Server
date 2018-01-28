﻿using System;
using System.Linq;
using FluentAssertions;
using Grumpy.Common;
using Grumpy.Entity;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;
using Grumpy.RipplesMQ.Infrastructure.Repositories;
using Xunit;

namespace Grumpy.RipplesMQ.Infrastructure.IntegrationTests
{
    public class SubscriberRepositoryTests 
    {
        private readonly EntityConnectionConfig _entityConnectionConfig;
        private const string ServerName = "MyServer";
        private readonly IRepositoriesFactory _repositoriesFactory;
        private readonly string _queueName;

        public SubscriberRepositoryTests()
        {
            _queueName = UniqueKeyUtility.Generate();
            _entityConnectionConfig = new EntityConnectionConfig(new DatabaseConnectionConfig(@"(localdb)\MSSQLLocalDB", "Grumpy.RipplesMQ.Database_Model"));
            _repositoriesFactory = new RepositoriesFactory(_entityConnectionConfig);
        }

        [Fact]
        public void CanUseSubscriberRepository()
        {
            try
            {
                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.SubscriberRepository();

                    cut.Get(ServerName, _queueName).Should().BeNull();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.SubscriberRepository();

                    cut.Insert(new Subscriber
                    {
                        ServerName = ServerName,
                        ServiceName = "MyServiceName",
                        Topic = "MyTopic",
                        Name = "MySubscriber",
                        RegisterDateTime = DateTimeOffset.Now,
                        QueueName = _queueName
                    });

                    repositories.Save();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.SubscriberRepository();

                    cut.Get(ServerName, _queueName).Should().NotBeNull();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.SubscriberRepository();

                    var entity = cut.Get(ServerName, _queueName);

                    entity.ServiceName = "MyServiceName";
                    entity.Topic = "AnotherTopic";
                    entity.Name = "MySubscriber";
                    entity.RegisterDateTime = DateTimeOffset.Now;

                    repositories.Save();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.SubscriberRepository();

                    cut.Get(ServerName, _queueName).Topic.Should().Be("AnotherTopic");
                    cut.GetAll().Count().Should().BeGreaterOrEqualTo(1);
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.SubscriberRepository();

                    cut.Delete(ServerName, _queueName);

                    repositories.Save();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.SubscriberRepository();

                    cut.Get(ServerName, _queueName).Should().BeNull();
                }
            }
            finally
            {
                using (var entities = new Entities(_entityConnectionConfig))
                {
                    entities.Subscriber.RemoveRange(entities.Subscriber.Where(e => e.QueueName == _queueName));
                    entities.SaveChanges();
                }
            }
        }
    }
}
