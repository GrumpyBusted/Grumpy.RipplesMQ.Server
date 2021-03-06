﻿using System;

namespace Grumpy.RipplesMQ.Core.Messages
{
    /// <summary>
    /// Locale Request Handler
    /// </summary>
    public class LocaleRequestHandler
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Service Name
        /// </summary>
        public string ServiceName { get; set; }
        
        /// <summary>
        /// Message Type
        /// </summary>
        public string RequestType { get; set; }

        /// <summary>
        /// Message Type
        /// </summary>
        public string ResponseType { get; set; }

        /// <summary>
        /// Queue name
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Handshake Date Time
        /// </summary>
        public DateTimeOffset HandshakeDateTime { get; set; }
    }
}