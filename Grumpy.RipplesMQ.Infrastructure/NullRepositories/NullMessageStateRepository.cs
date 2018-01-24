using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Entities;
using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    public class NullMessageStateRepository : IMessageStateRepository
    {
        public void Insert(MessageState messageState)
        {
        }

        public MessageState Get(string messageId, string subscriberName)
        {
            return null;
        }

        public IEnumerable<MessageState> GetAll()
        {
            return Enumerable.Empty<MessageState>();
        }

        public void DeleteBySubscriber(string subscriberName)
        {
        }

        public void DeleteByMessageId(string messageId)
        {
        }
    }
}