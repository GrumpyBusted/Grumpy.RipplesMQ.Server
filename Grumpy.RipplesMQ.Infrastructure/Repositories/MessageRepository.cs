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

        public void Insert(Message message)
        {
            _entities.Message.Add(message);
        }

        public void Delete(string id)
        {
            _entities.Message.RemoveRange(_entities.Message.Where(m => m.Id == id));
        }

        public IEnumerable<Message> GetAll()
        {
            return _entities.Message.ToList();
        }
    }
}