using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    /// <inheritdoc />
    public class NullRepositoryContextFactory : IRepositoryContextFactory
    {
        /// <inheritdoc />
        public IRepositoryContext Get()
        {
            return new NullRepositoryContext();
        }
    }
}