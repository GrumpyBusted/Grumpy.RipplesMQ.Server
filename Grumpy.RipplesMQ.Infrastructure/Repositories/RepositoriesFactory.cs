using Grumpy.Entity.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    /// <inheritdoc />
    public class RepositoriesFactory : IRepositoriesFactory
    {
        /// <inheritdoc />
        public RepositoriesFactory(ILogger logger, IEntityConnectionConfig entityConnectionConfig)
        {
            _logger = logger;
            _entityConnectionConfig = entityConnectionConfig;
        }

        private readonly IEntityConnectionConfig _entityConnectionConfig;

        private readonly ILogger _logger;

        /// <inheritdoc />
        public IRepositories Create()
        {
            return new Repositories(_logger, _entityConnectionConfig); 
        }
    }
}