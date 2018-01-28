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
        private readonly IRepositories _repositories;
        private readonly IMessageRepository _cut;

        public NullMessageRepositoryTests()
        {
            _repositories = new NullRepositoriesFactory().Create();
            _cut = _repositories.MessageRepository();
        }

        public void Dispose()
        {
            _repositories.Dispose();
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
