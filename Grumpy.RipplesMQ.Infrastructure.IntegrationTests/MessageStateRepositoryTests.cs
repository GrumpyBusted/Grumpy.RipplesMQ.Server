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
    public class MessageStateRepositoryTests 
    {
        private readonly EntityConnectionConfig _entityConnectionConfig;
        private readonly string _subscriberName;
        private readonly IRepositoriesFactory _repositoriesFactory;
        private readonly string _messageId;

        public MessageStateRepositoryTests()
        {
            _messageId = UniqueKeyUtility.Generate();
            _subscriberName = UniqueKeyUtility.Generate();
            _entityConnectionConfig = new EntityConnectionConfig(new DatabaseConnectionConfig(@"(localdb)\MSSQLLocalDB", "Grumpy.RipplesMQ.Database_Model"));
            _repositoriesFactory = new RepositoriesFactory(NullLogger.Instance, _entityConnectionConfig);
        }

        [Fact]
        public void CanUseMessageStateRepository()
        {
            try
            {
                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageStateRepository();

                    cut.Get(_messageId, _subscriberName).Should().BeNull();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageStateRepository();

                    cut.Insert(new MessageState
                    {
                        MessageId = _messageId,
                        SubscriberName = _subscriberName,
                        State = "Unknown",
                        ErrorCount = 0,
                        UpdateDateTime = DateTimeOffset.Now
                    });

                    repositories.Save();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageStateRepository();

                    cut.Get(_messageId, _subscriberName).Should().NotBeNull();
                    cut.GetAll().Where(e => e.MessageId == _messageId).Should().NotBeNull();
                    cut.GetAll().Count().Should().BeGreaterOrEqualTo(1);
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageStateRepository();

                    cut.DeleteByMessageId(_messageId);

                    repositories.Save();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageStateRepository();

                    cut.Get(_messageId, _subscriberName).Should().BeNull();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageStateRepository();

                    cut.Insert(new MessageState
                    {
                        MessageId = _messageId,
                        SubscriberName = _subscriberName,
                        State = "Unknown",
                        ErrorCount = 0,
                        UpdateDateTime = DateTimeOffset.Now
                    });

                    repositories.Save();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageStateRepository();

                    cut.DeleteBySubscriber(_subscriberName);

                    repositories.Save();
                }

                using (var repositories = _repositoriesFactory.Create())
                {
                    var cut = repositories.MessageStateRepository();

                    cut.Get(_messageId, _subscriberName).Should().BeNull();
                }
            }
            finally
            {
                using (var entities = new Entities(_entityConnectionConfig))
                {
                    entities.MessageState.RemoveRange(entities.MessageState.Where(e => e.MessageId == _messageId));
                    entities.SaveChanges();
                }
            }
        }
    }
}
