using System;
using System.Linq;
using Grumpy.Json;
using Grumpy.RipplesMQ.Core.Entities;
using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    internal class MessageStateRepository : IMessageStateRepository
    {
        public void Insert(MessageState messageState)
        {
            Console.WriteLine($"Insert in MessageStateRepository {messageState.SerializeToJson(false)}");
        }

        public MessageState Get(string messageId, string subscriberName)
        {
            return GetAll().SingleOrDefault(e => e.MessageId == messageId && e.SubscriberName == subscriberName);
        }

        public IQueryable<MessageState> GetAll()
        {
            return Enumerable.Empty<MessageState>().AsQueryable();
        }

        public void DeleteBySubscriber(string subscriberName)
        {
            Console.WriteLine($"Delete from MessageStateRepository - SubscriberName: {subscriberName}");
        }

        public void DeleteByMessageId(string messageId)
        {
            Console.WriteLine($"Delete from MessageStateRepository - MessageId: {messageId}");
        }
    }
}