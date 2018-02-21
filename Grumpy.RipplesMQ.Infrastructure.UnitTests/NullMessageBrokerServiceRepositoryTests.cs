using System;
using System.Linq;
using FluentAssertions;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;
using Grumpy.RipplesMQ.Infrastructure.NullRepositories;
using Xunit;

namespace Grumpy.RipplesMQ.Infrastructure.UnitTests
{
    public class NullMessageBrokerServiceRepositoryTests : IDisposable
    {
        private readonly IRepositoryContext _repositoryContext;
        private readonly IMessageBrokerServiceRepository _cut;

        public NullMessageBrokerServiceRepositoryTests()
        {
            _repositoryContext = new NullRepositoryContextFactory().Get();
            _cut = _repositoryContext.MessageBrokerServiceRepository;
        }

        public void Dispose()
        {
            _repositoryContext.Dispose();
        }

        [Fact]
        public void CanInsertInNullMessageBrokerServiceRepository()
        {
            _cut.Insert(new MessageBrokerService());
        }

        [Fact]
        public void CanDeleteInNullMessageBrokerServiceRepository()
        {
            _cut.Delete("", "");
        }

        [Fact]
        public void GetFromNullMessageBrokerServiceRepositoryShouldReturnNull()
        {
            _cut.Get("MyServerName", "MyServiceName").Should().BeNull();
        }

        [Fact]
        public void GetAllFromNullMessageBrokerServiceRepositoryShouldZeroElements()
        {
            _cut.GetAll().Count().Should().Be(0);
        }
    }
}
