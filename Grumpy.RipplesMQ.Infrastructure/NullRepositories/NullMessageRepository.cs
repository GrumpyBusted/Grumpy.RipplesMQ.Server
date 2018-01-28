using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    /// <inheritdoc />
    public class NullMessageRepository : IMessageRepository
    {
        /// <inheritdoc />
        public void Insert(Message message)
        {
        }

        /// <inheritdoc />
        public void Delete(string id)
        {
        }

        /// <inheritdoc />
        public IEnumerable<Message> GetAll()
        {
            return Enumerable.Empty<Message>();
        }
    }
}