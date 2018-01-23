using System.Collections.Generic;
using Grumpy.RipplesMQ.Core.Entities;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    public interface IMessageStateRepository
    {
        void Insert(MessageState messageState);
        MessageState Get(string messageId, string subscriberName);
        IEnumerable<MessageState> GetAll();
        void DeleteBySubscriber(string subscriberName);
        void DeleteByMessageId(string messageId);
    }
}