using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    /// <inheritdoc />
    public class NullRepositoriesFactory : IRepositoriesFactory
    {
        /// <inheritdoc />
        public IRepositories Create()
        {
            return new NullRepositories();
        }
    }
}