using Grumpy.Entity.Interfaces;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    /// <inheritdoc />
    public class RepositoryContextFactory : IRepositoryContextFactory
    {
        private readonly IEntityConnectionConfig _entityConnectionConfig;
        private readonly ILogger _logger;

        /// <inheritdoc />
        public RepositoryContextFactory(ILogger logger, IEntityConnectionConfig entityConnectionConfig)
        {
            _logger = logger;
            _entityConnectionConfig = entityConnectionConfig;
        }

        /// <inheritdoc />
        public IRepositoryContext Get()
        {
            return new RepositoryContext(_logger, _entityConnectionConfig); 
        }
    }
}