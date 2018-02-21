using FluentAssertions;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Infrastructure.NullRepositories;
using Xunit;

namespace Grumpy.RipplesMQ.Infrastructure.UnitTests
{
    public class NullRepositoriesTests
    {
        private readonly IRepositoryContextFactory _repositoryContextFactory;

        public NullRepositoriesTests()
        {
            _repositoryContextFactory = new NullRepositoryContextFactory();
        }

        [Fact]
        public void CanCreateNullRepositories()
        {
            _repositoryContextFactory.Get().GetType().Should().Be<NullRepositoryContext>();
        }

        [Fact]
        public void CanSaveNullRepositories()
        {
            using (var repositoryContext = _repositoryContextFactory.Get())
            {
                repositoryContext.Save();
            }
        }
    }
}
