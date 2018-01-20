﻿using Grumpy.Common.ToBe;
using Topshelf;

namespace Grumpy.RipplesMQ.Server
{
    public static class Program
    {
        private static void Main()
        {
            HostFactory.Run(TopshelfUtility.BuildService(MessageBrokerServiceBuilder.Build));
        }
    }
}