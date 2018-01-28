using FluentAssertions;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Infrastructure.NullRepositories;
using Xunit;

namespace Grumpy.RipplesMQ.Infrastructure.UnitTests
{
    public class NullRepositoriesTests
    {
        private readonly IRepositoriesFactory _repositoriesFactory;

        public NullRepositoriesTests()
        {
            _repositoriesFactory = new NullRepositoriesFactory();
        }

        [Fact]
        public void CanCreateNullRepositories()
        {
            _repositoriesFactory.Create().GetType().Should().Be<NullRepositories.NullRepositories>();
        }

        [Fact]
        public void CanSaveNullRepositories()
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                repositories.Save();
            }
        }
    }
}
