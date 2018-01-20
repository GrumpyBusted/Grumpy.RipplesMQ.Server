using System.Linq;
using Grumpy.RipplesMQ.Core.Entities;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    public interface ISubscriberRepository
    {
        void Insert(Subscriber subscriber);
        void Update(Subscriber subscriber);
        Subscriber Get(string serverName, string queueName);
        IQueryable<Subscriber> GetAll();
    }
}