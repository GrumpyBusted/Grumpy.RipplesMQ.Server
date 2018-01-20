using System.Linq;
using Grumpy.RipplesMQ.Core.Entities;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    public interface IMessageStateRepository
    {
        void Insert(MessageState messageState);
        MessageState Get(string messageId, string subscriberName);
        IQueryable<MessageState> GetAll();
        void DeleteBySubscriber(string subscriberName);
        void DeleteByMessageId(string messageId);
    }
}