using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Entities;
using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    public class NullSubscriberRepository : ISubscriberRepository
    {
        public void Insert(Subscriber subscriber)
        {
        }

        public void Update(Subscriber subscriber)
        {
        }

        public Subscriber Get(string serverName, string queueName)
        {
            return null;
        }

        public IEnumerable<Subscriber> GetAll()
        {
            return Enumerable.Empty<Subscriber>();
        }
    }
}