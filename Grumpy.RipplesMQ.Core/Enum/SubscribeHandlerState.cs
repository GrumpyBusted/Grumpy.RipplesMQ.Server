namespace Grumpy.RipplesMQ.Core.Enum
{
    internal enum SubscribeHandlerState
    {
        Published,
        Distributed,
        SendToServer,
        SendToSubscriber,
        Completed,
        Error
    }
}