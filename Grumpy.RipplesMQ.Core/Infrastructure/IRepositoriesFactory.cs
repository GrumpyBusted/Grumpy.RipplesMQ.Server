namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    /// <summary>
    /// Factory for Repositories for Message Broker
    /// </summary>
    public interface IRepositoriesFactory 
    {
        /// <summary>
        /// Creates Instance of Message Broker Repositories
        /// </summary>
        /// <returns>The Instance</returns>
        IRepositories Create();
    }
}