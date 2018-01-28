using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    /// <inheritdoc />
    public class NullSubscriberRepository : ISubscriberRepository
    {
        /// <inheritdoc />
        public void Insert(Subscriber subscriber)
        {
        }

        /// <inheritdoc />
        public Subscriber Get(string serverName, string queueName)
        {
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<Subscriber> GetAll()
        {
            return Enumerable.Empty<Subscriber>();
        }

        /// <inheritdoc />
        public void Delete(string serverName, string queueName)
        {
        }
    }
}