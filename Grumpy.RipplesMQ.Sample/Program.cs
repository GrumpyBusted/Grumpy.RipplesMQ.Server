using Grumpy.ServiceBase;

namespace Grumpy.RipplesMQ.Sample
{
    public static class Program
    {
        private static void Main()
        {
            TopshelfUtility.Run<MessageBrokerService>();
        }
    }
}