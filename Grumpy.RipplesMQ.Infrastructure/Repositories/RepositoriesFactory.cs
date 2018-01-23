using Grumpy.Entity.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    public class RepositoriesFactory : IRepositoriesFactory
    {
        private readonly IEntityConnectionConfig _entityConnectionConfig;

        public RepositoriesFactory(IEntityConnectionConfig entityConnectionConfig)
        {
            _entityConnectionConfig = entityConnectionConfig;
        }

        public IRepositories Create()
        {
            return new Repositories(_entityConnectionConfig); 
        }
    }
}