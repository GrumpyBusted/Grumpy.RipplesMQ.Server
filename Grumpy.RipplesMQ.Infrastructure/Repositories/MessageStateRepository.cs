using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    internal class MessageStateRepository : IMessageStateRepository
    {
        private readonly Entities _entities;

        public MessageStateRepository(Entities entities)
        {
            _entities = entities;
        }

        public void Insert(Core.Entities.MessageState messageState)
        {
            _entities.MessageState.Add(new MessageState
            {
                MessageId = messageState.MessageId,
                SubscriberName = messageState.SubscriberName,
                State = messageState.State,
                ErrorCount = messageState.ErrorCount,
                UpdateDateTime = messageState.UpdateDateTime
            });
        }

        public Core.Entities.MessageState Get(string messageId, string subscriberName)
        {
            return FromEntity(GetAllCurrent().SingleOrDefault(e => e.MessageId == messageId && e.SubscriberName == subscriberName));
        }

        public IEnumerable<Core.Entities.MessageState> GetAll()
        {
            return GetAllCurrent().ToList().Select(FromEntity);
        }

        public void DeleteBySubscriber(string subscriberName)
        {
            _entities.MessageState.RemoveRange(_entities.MessageState.Where(e => e.SubscriberName == subscriberName));
        }

        public void DeleteByMessageId(string messageId)
        {
            _entities.MessageState.RemoveRange(_entities.MessageState.Where(e => e.MessageId == messageId));
        }

        private IQueryable<MessageState> GetAllCurrent()
        {
            return _entities.MessageState.Where(e => e.Id == _entities.MessageState.Where(m => m.MessageId == e.MessageId && m.SubscriberName == e.SubscriberName).Select(s => s.Id).Max());
        }

        private static Core.Entities.MessageState FromEntity(MessageState messageState)
        {
            return messageState == null ? null : new Core.Entities.MessageState
            {
                MessageId = messageState.MessageId,
                SubscriberName = messageState.SubscriberName,
                State = messageState.State,
                ErrorCount = messageState.ErrorCount,
                UpdateDateTime = messageState.UpdateDateTime
            };
        }
    }
}