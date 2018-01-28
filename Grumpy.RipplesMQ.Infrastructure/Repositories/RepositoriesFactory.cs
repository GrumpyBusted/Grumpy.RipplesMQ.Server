using Grumpy.Entity.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    /// <inheritdoc />
    public class RepositoriesFactory : IRepositoriesFactory
    {
        private readonly IEntityConnectionConfig _entityConnectionConfig;

        /// <inheritdoc />
        public RepositoriesFactory(IEntityConnectionConfig entityConnectionConfig)
        {
            _entityConnectionConfig = entityConnectionConfig;
        }

        /// <inheritdoc />
        public IRepositories Create()
        {
            return new Repositories(_entityConnectionConfig); 
        }
    }
}