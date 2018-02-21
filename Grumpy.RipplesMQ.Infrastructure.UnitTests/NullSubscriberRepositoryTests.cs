using System;
using System.Linq;
using FluentAssertions;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;
using Grumpy.RipplesMQ.Infrastructure.NullRepositories;
using Xunit;

namespace Grumpy.RipplesMQ.Infrastructure.UnitTests
{
    public class NullSubscriberRepositoryTests : IDisposable
    {
        private readonly IRepositoryContext _repositoryContext;
        private readonly ISubscriberRepository _cut;

        public NullSubscriberRepositoryTests()
        {
            _repositoryContext = new NullRepositoryContextFactory().Get();
            _cut = _repositoryContext.SubscriberRepository;
        }

        public void Dispose()
        {
            _repositoryContext.Dispose();
        }

        [Fact]
        public void CanInsertInNullSubscriberRepository()
        {
            _cut.Insert(new Subscriber());
        }

        [Fact]
        public void CanDeleteInNullSubscriberRepository()
        {
            _cut.Delete("", "");
        }

        [Fact]
        public void GetFromNullSubscriberRepositoryShouldReturnNull()
        {
            _cut.Get("MyServerName", "MyQueueName").Should().BeNull();
        }

        [Fact]
        public void GetAllFromNullSubscriberRepositoryShouldZeroElements()
        {
            _cut.GetAll().Count().Should().Be(0);
        }
    }
}
