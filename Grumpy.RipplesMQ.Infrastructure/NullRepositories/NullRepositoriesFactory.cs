using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    public class NullRepositoriesFactory : IRepositoriesFactory
    {
        public IRepositories Create()
        {
            return new NullRepositories();
        }
    }
}