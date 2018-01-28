using System.Collections.Generic;
using Grumpy.RipplesMQ.Entity;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    /// <summary>
    /// Message Broker Service Repository
    /// </summary>
    public interface IMessageBrokerServiceRepository
    {
        /// <summary>
        /// Insert new Message Broker Service
        /// </summary>
        /// <param name="messageBrokerService">Message Broker Service</param>
        void Insert(MessageBrokerService messageBrokerService);
        
        /// <summary>
        /// Get Message Broker Service
        /// </summary>
        /// <param name="serverName">Server Name</param>
        /// <param name="serviceName">Service name</param>
        /// <returns>The Message Broker Service</returns>
        MessageBrokerService Get(string serverName, string serviceName);
        
        /// <summary>
        /// Get all Message Broker Services
        /// </summary>
        /// <returns>Message Broker Services</returns>
        IEnumerable<MessageBrokerService> GetAll();

        /// <summary>
        /// Delete Message Broker Service
        /// </summary>
        /// <param name="serverName">Server Name</param>
        /// <param name="serviceName">Service Name</param>
        void Delete(string serverName, string serviceName);
    }
}