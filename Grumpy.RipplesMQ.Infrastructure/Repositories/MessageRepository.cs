using System;
using System.Linq;
using Grumpy.Json;
using Grumpy.RipplesMQ.Core.Entities;
using Grumpy.RipplesMQ.Core.Infrastructure;

namespace Grumpy.RipplesMQ.Infrastructure.Repositories
{
    internal class MessageRepository : IMessageRepository
    {
        public void Insert(Message message)
        {
            Console.WriteLine($"Insert in MessageRepository {message.SerializeToJson(false)}");
        }

        public void Delete(string id)
        {
            Console.WriteLine($"Delete from MessageRepository {id}");
        }

        public Message Get(string id)
        {
            return GetAll().SingleOrDefault(e => e.Id == id);
        }

        public IQueryable<Message> GetAll()
        {
            return Enumerable.Empty<Message>().AsQueryable();
        }
    }
}