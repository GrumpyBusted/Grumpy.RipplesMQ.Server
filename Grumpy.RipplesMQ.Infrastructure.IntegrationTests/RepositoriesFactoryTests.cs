using FluentAssertions;
using Grumpy.Entity.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Infrastructure.Repositories;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Infrastructure.IntegrationTests
{
    public class RepositoriesFactoryTests
    {
        private readonly IRepositoriesFactory _repositoriesFactory;

        public RepositoriesFactoryTests()
        {
            var config = Substitute.For<IEntityConnectionConfig>();
            config.ConnectionString(Arg.Any<string>(), Arg.Any<string>()).Returns("metadata=res://*/Model.csdl|res://*/Model.ssdl|res://*/Model.msl;provider=System.Data.SqlClient;provider connection string=\"data source=(localdb)\\MSSQLLocalDB;initial catalog=Grumpy.RipplesMQ.Database_Model;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework\"\" providerName=\"System.Data.EntityClient");

            _repositoriesFactory = new RepositoriesFactory(config);
        }

        [Fact]
        public void CanCreateRepositories()
        {
            _repositoriesFactory.Create().GetType().Should().Be<Repositories.Repositories>();
        }

        [Fact]
        public void CanSaveRepositories()
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                repositories.Save();
            }
        }
    }
}
