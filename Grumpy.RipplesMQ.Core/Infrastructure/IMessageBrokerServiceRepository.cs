﻿using System.Collections.Generic;

namespace Grumpy.RipplesMQ.Core.Infrastructure
{
    public interface IMessageBrokerServiceRepository
    {
        void Insert(Entities.MessageBrokerService messageBroker);
        void Update(Entities.MessageBrokerService messageBroker);
        Entities.MessageBrokerService Get(string serverName, string serviceName, string instanceName);
        IEnumerable<Entities.MessageBrokerService> GetAll();
    }
}