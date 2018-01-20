using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using Grumpy.Common.ToBe.UnitTests.Helper;
using Xunit;

namespace Grumpy.Common.ToBe.UnitTests
{
    public class CancelableServiceBaseTests
    {
        [Fact]
        public void StopGracefully()
        {
            var cut = new MyCancelableService();
            cut.Start();
            Thread.Sleep(200);
            cut.Stop();
            cut.GracefulExit.Should().BeTrue();
        }

        [Fact]
        public void OneProcessCountPerSecond()
        {
            var cut = new MyCancelableService();
            Thread.Sleep(200);
            cut.Start();
            Thread.Sleep(2500);
            cut.Stop();
            cut.ProcessCount.Should().BeGreaterOrEqualTo(2);
        }

        [Fact]
        public void WaitForSleep()
        {
            var cut = new MyCancelableService();
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            cut.Start();
            Thread.Sleep(200);
            cut.Stop();
            stopWatch.Stop();
            stopWatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(900);
        }
    }
}
