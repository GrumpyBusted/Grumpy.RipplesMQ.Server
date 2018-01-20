namespace Grumpy.RipplesMQ.Core.Interfaces
{
    public interface IMessageBrokerFactory
    {
        IMessageBroker Create();
    }
}