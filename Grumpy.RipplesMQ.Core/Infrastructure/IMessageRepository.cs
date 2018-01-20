using System.Linq;
using Grumpy.RipplesMQ.Core.Entities;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    public interface IMessageRepository
    {
        void Insert(Message message);
        void Delete(string id);
        Message Get(string id);
        IQueryable<Message> GetAll();
    }
}