using System;
using System.Linq;
using FluentAssertions;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;
using Grumpy.RipplesMQ.Infrastructure.NullRepositories;
using Xunit;

namespace Grumpy.RipplesMQ.Infrastructure.UnitTests
{
    public class NullMessageRepositoryTests : IDisposable
    {
        private readonly IRepositoryContext _repositoryContext;
        private readonly IMessageRepository _cut;

        public NullMessageRepositoryTests()
        {
            _repositoryContext = new NullRepositoryContextFactory().Get();
            _cut = _repositoryContext.MessageRepository;
        }

        public void Dispose()
        {
            _repositoryContext.Dispose();
        }

        [Fact]
        public void CanInsertInNullMessageRepository()
        {
            _cut.Insert(new Message());
        }

        [Fact]
        public void CanDeleteInNullMessageRepository()
        {
            _cut.Delete("1");
        }

        [Fact]
        public void GetAllFromNullMessageRepositoryShouldZeroElements()
        {
            _cut.GetAll().Count().Should().Be(0);
        }
    }
}
