using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    public class RepositoriesFactory : IRepositoriesFactory
    {
        public IRepositories Create()
        {
            return new Repositories(); 
        }
    }
}