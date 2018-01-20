using System;
using Grumpy.Common.Extensions;
using Topshelf;
using Topshelf.HostConfigurators;

namespace Grumpy.Common.ToBe
{
    public static class TopshelfUtility
    {
        public static Action<HostConfigurator> BuildService<T>(Func<string, T> serviceBuilder) where T : class, ITopshelfService
        {
            var assemblyInfo = new AssemblyInfoUtility();

            return x =>
            {
                x.Service<T>(s =>
                {
                    s.ConstructUsing(serviceBuilder);
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();
                x.SetDescription(assemblyInfo.Description);
                x.SetDisplayName(assemblyInfo.Title + (assemblyInfo.Version.NullOrEmpty() ? "" : $" (Version: {assemblyInfo.Version})"));
                x.SetServiceName(assemblyInfo.Title);
            };
        }
    }
}
