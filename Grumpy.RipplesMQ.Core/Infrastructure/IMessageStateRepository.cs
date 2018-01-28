using System.Collections.Generic;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    /// <summary>
    /// Message/Subscriber State Repository
    /// </summary>
    public interface IMessageStateRepository
    {
        /// <summary>
        /// Insert new State for Message/Subscriber
        /// </summary>
        /// <param name="messageState"></param>
        void Insert(MessageState messageState);

        /// <summary>
        /// Get current state of a Message for a Subscriber
        /// </summary>
        /// <param name="messageId">Message Id</param>
        /// <param name="subscriberName">Subscriber Name</param>
        /// <returns>Message/Subscriber State</returns>
        MessageState Get(string messageId, string subscriberName);

        /// <summary>
        /// Get all Message/Subscriber
        /// </summary>
        /// <returns></returns>
        IEnumerable<MessageState> GetAll();
        
        /// <summary>
        /// Delete all Messages States for a specified Subscriber 
        /// </summary>
        /// <param name="subscriberName">Subscriber Name</param>
        
        void DeleteBySubscriber(string subscriberName);
        /// <summary>
        /// Delete all Subscriber States for a specified Message
        /// </summary>
        /// <param name="messageId">Message Id</param>
        void DeleteByMessageId(string messageId);
    }
}