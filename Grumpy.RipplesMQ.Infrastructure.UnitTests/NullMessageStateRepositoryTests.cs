using System;
using System.Linq;
using FluentAssertions;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;
using Grumpy.RipplesMQ.Infrastructure.NullRepositories;
using Xunit;

namespace Grumpy.RipplesMQ.Infrastructure.UnitTests
{
    public class NullMessageStateRepositoryTests : IDisposable
    {
        private readonly IRepositories _repositories;
        private readonly IMessageStateRepository _cut;

        public NullMessageStateRepositoryTests()
        {
            _repositories = new NullRepositoriesFactory().Create();
            _cut = _repositories.MessageStateRepository();
        }

        public void Dispose()
        {
            _repositories.Dispose();
        }

        [Fact]
        public void CanInsertInNullMessageStateRepository()
        {
            _cut.Insert(new MessageState());
        }

        [Fact]
        public void CanDeleteByMessageIdFromNullMessageStateRepository()
        {
            _cut.DeleteByMessageId("MessageId");
        }

        [Fact]
        public void CanDeleteBySubscriberFromNullMessageStateRepository()
        {
            _cut.DeleteBySubscriber("MySubscriberName");
        }

        [Fact]
        public void GetFromNullMessageStateRepositoryShouldReturnNull()
        {
            _cut.Get("MessageId", "MySubscriberName").Should().BeNull();
        }

        [Fact]
        public void GetAllFromNullMessageStateRepositoryShouldZeroElements()
        {
            _cut.GetAll().Count().Should().Be(0);
        }
    }
}
