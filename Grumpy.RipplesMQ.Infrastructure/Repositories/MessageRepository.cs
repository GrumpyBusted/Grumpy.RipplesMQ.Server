using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    internal class MessageRepository : IMessageRepository
    {
        private readonly Entities _entities;

        public MessageRepository(Entities entities)
        {
            _entities = entities;
        }

        public void Insert(Core.Entities.Message message)
        {
            _entities.Message.Add(new Message
            {
                Id = message.Id,
                Topic = message.Topic,
                Type = message.Type,
                Body = message.Body,
                PublishDateTime = message.PublishDateTime
            });
        }

        public void Delete(string id)
        {
            _entities.Message.RemoveRange(_entities.Message.Where(m => m.Id == id));
        }

        public IEnumerable<Core.Entities.Message> GetAll()
        {
            return _entities.Message.ToList().Select(FromEntity);
        }

        private static Core.Entities.Message FromEntity(Message message)
        {
            return message == null ? null : new Core.Entities.Message
            {
                Id = message.Id,
                Topic = message.Topic,
                Type = message.Type,
                Body = message.Body,
                PublishDateTime = message.PublishDateTime
            };
        }
    }
}