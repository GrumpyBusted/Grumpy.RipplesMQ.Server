using FluentAssertions;
using Grumpy.Entity.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Infrastructure.IntegrationTests
{
    public class RepositoriesFactoryTests
    {
        private readonly IRepositoryContextFactory _repositoryContextFactory;

        public RepositoriesFactoryTests()
        {
            var config = Substitute.For<IEntityConnectionConfig>();
            config.ConnectionString(Arg.Any<string>(), Arg.Any<string>()).Returns("metadata=res://*/Model.csdl|res://*/Model.ssdl|res://*/Model.msl;provider=System.Data.SqlClient;provider connection string=\"data source=(localdb)\\MSSQLLocalDB;initial catalog=Grumpy.RipplesMQ.Database_Model;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework\"\" providerName=\"System.Data.EntityClient");

            _repositoryContextFactory = new RepositoryContextFactory(NullLogger.Instance, config);
        }

        [Fact]
        public void CanCreateRepositories()
        {
            _repositoryContextFactory.Get().GetType().Should().Be<RepositoryContext>();
        }

        [Fact]
        public void CanSaveRepositories()
        {
            using (var repositoryContext = _repositoryContextFactory.Get())
            {
                repositoryContext.Save();
            }
        }
    }
}
