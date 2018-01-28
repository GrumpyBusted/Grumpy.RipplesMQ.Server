using System.Collections.Generic;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    /// <summary>
    /// Message Repository
    /// </summary>
    public interface IMessageRepository
    {
        /// <summary>
        /// Insert new Message
        /// </summary>
        /// <param name="message">The Message</param>
        void Insert(Message message);

        /// <summary>
        /// Delete a Message by Id
        /// </summary>
        /// <param name="id"></param>
        void Delete(string id);
       
        /// <summary>
        /// Get all Messages
        /// </summary>
        /// <returns></returns>
        IEnumerable<Message> GetAll();
    }
}