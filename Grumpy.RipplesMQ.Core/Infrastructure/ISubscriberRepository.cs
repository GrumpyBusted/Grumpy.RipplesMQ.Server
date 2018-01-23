using System.Collections.Generic;
using Grumpy.RipplesMQ.Core.Entities;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    public interface ISubscriberRepository
    {
        void Insert(Subscriber subscriber);
        void Update(Subscriber subscriber);
        Subscriber Get(string serverName, string queueName);
        IEnumerable<Subscriber> GetAll();
    }
}