using System.Collections.Generic;
using System.Linq;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Infrastructure.NullRepositories
{
    /// <inheritdoc />
    public class NullMessageStateRepository : IMessageStateRepository
    {
        /// <inheritdoc />
        public void Insert(MessageState messageState)
        {
        }

        /// <inheritdoc />
        public MessageState Get(string messageId, string subscriberName)
        {
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<MessageState> GetAll()
        {
            return Enumerable.Empty<MessageState>();
        }

        /// <inheritdoc />
        public void DeleteBySubscriber(string subscriberName)
        {
        }

        /// <inheritdoc />
        public void DeleteByMessageId(string messageId)
        {
        }
    }
}