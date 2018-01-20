using System;

namespace Grumpy.Common.ToBe
{
    public interface ICancelableServiceBase : IDisposable
    {
        void Start();
        void StartSync();
        void Stop();
    }
}