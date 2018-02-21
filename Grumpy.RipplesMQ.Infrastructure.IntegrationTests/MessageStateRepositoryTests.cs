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
        private readonly IRepositoryContextFactory _repositoryContextFactory;
        private readonly string _messageId;

        public MessageStateRepositoryTests()
        {
            _messageId = UniqueKeyUtility.Generate();
            _subscriberName = UniqueKeyUtility.Generate();
            _entityConnectionConfig = new EntityConnectionConfig(new DatabaseConnectionConfig(@"(localdb)\MSSQLLocalDB", "Grumpy.RipplesMQ.Database_Model"));
            _repositoryContextFactory = new RepositoryContextFactory(NullLogger.Instance, _entityConnectionConfig);
        }

        [Fact]
        public void CanUseMessageStateRepository()
        {
            try
            {
                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageStateRepository;

                    cut.Get(_messageId, _subscriberName).Should().BeNull();
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageStateRepository;

                    cut.Insert(new MessageState
                    {
                        MessageId = _messageId,
                        SubscriberName = _subscriberName,
                        State = "Unknown",
                        ErrorCount = 0,
                        UpdateDateTime = DateTimeOffset.Now
                    });

                    repositoryContext.Save();
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageStateRepository;

                    cut.Get(_messageId, _subscriberName).Should().NotBeNull();
                    cut.GetAll().Where(e => e.MessageId == _messageId).Should().NotBeNull();
                    cut.GetAll().Count().Should().BeGreaterOrEqualTo(1);
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageStateRepository;

                    cut.DeleteByMessageId(_messageId);

                    repositoryContext.Save();
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageStateRepository;

                    cut.Get(_messageId, _subscriberName).Should().BeNull();
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageStateRepository;

                    cut.Insert(new MessageState
                    {
                        MessageId = _messageId,
                        SubscriberName = _subscriberName,
                        State = "Unknown",
                        ErrorCount = 0,
                        UpdateDateTime = DateTimeOffset.Now
                    });

                    repositoryContext.Save();
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageStateRepository;

                    cut.DeleteBySubscriber(_subscriberName);

                    repositoryContext.Save();
                }

                using (var repositoryContext = _repositoryContextFactory.Get())
                {
                    var cut = repositoryContext.MessageStateRepository;

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
