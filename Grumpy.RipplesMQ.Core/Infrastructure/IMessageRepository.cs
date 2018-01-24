using System.Collections.Generic;
using Grumpy.RipplesMQ.Core.Entities;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    public interface IMessageRepository
    {
        void Insert(Message message);
        void Delete(string id);
        IEnumerable<Message> GetAll();
    }
}