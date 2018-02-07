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
    public class MessageRepositoryTests
    {
        private readonly EntityConnectionConfig _entityConnectionConfig;
        private readonly IRepositoriesFactory _repositoriesFactory;
        private readonly string _messageId;

        public MessageRepositoryTests()
        {
            _messageId = UniqueKeyUtility.Generate();
            _entityConnectionConfig = new EntityConnectionConfig(new DatabaseConnectionConfig(@"(localdb)\MSSQLLocalDB", "Grumpy.RipplesMQ.Database_Model"));
            _repositoriesFactory = new RepositoriesFactory(NullLogger.Instance, _entityConnectionConfig);
        }

        [Fact]
        public void CanUseMessageRepository()
        {
            try
            {
                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageRepository();

                    cut.GetAll().Where(e => e.Id == _messageId).Should().BeEmpty();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageRepository();

                    cut.Insert(new Message
                    {
                        Id = _messageId,
                        Topic = "MyTopic",
                        Type = "MyType",
                        Body = "MyMessage",
                        PublishDateTime = DateTimeOffset.Now
                    });

                    repositories.Save();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageRepository();

                    cut.GetAll().Where(e => e.Id == _messageId).Should().NotBeEmpty();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageRepository();

                    cut.Delete(_messageId);

                    repositories.Save();
                }
            }
            finally
            {
                using (var entities = new Entities(_entityConnectionConfig))
                {
                    entities.Message.RemoveRange(entities.Message.Where(e => e.Id == _messageId));
                    entities.SaveChanges();
                }
            }
        }
    }
}
