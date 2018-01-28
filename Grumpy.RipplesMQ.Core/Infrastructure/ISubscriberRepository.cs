using System.Collections.Generic;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    /// <summary>
    /// Subscriber Repository
    /// </summary>
    public interface ISubscriberRepository
    {
        /// <summary>
        /// Insert new Subscriber
        /// </summary>
        /// <param name="subscriber"></param>
        void Insert(Subscriber subscriber);

        /// <summary>
        /// Get the Subscriber
        /// </summary>
        /// <param name="serverName">Server Name</param>
        /// <param name="queueName">Queue Name</param>
        /// <returns>Subscriber</returns>
        Subscriber Get(string serverName, string queueName);
        
        /// <summary>
        /// All subscribers
        /// </summary>
        /// <returns></returns>
        IEnumerable<Subscriber> GetAll();

        /// <summary>
        /// Delete Subscriber
        /// </summary>
        /// <param name="serverName">Server Name</param>
        /// <param name="queueName">Queue Name</param>
        void Delete(string serverName, string queueName);
    }
}