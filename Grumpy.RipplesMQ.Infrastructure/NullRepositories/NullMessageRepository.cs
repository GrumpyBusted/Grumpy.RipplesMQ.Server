using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Entities;
using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    public class NullMessageRepository : IMessageRepository
    {
        public void Insert(Message message)
        {
        }

        public void Delete(string id)
        {
        }

        public IEnumerable<Message> GetAll()
        {
            return Enumerable.Empty<Message>();
        }
    }
}