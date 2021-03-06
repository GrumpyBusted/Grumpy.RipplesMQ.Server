﻿using System;
using Grumpy.MessageQueue.Interfaces;

namespace Grumpy.RipplesMQ.Core.Dto
{
    /// <summary>
    /// Request Handler
    /// </summary>
    public class RequestHandler
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Server Name
        /// </summary>
        public string ServerName { get; set; }
 
        /// <summary>
        /// Server Name
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Queue Name 
        /// </summary>
        public string QueueName { get; set; }
        
        /// <summary>
        /// Handshake Timestamp
        /// </summary>
        public DateTimeOffset? HandshakeDateTime { get; set; }
        
        /// <summary>
        /// Queue
        /// </summary>
        public IQueue Queue { get; set; }

        /// <summary>
        /// Request Message Type
        /// </summary>
        public string RequestType { get; set; }
 
        /// <summary>
        /// Response Message Type
        /// </summary>
        public string ResponseType { get; set; }
    }
}