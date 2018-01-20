using System;
using System.Reflection;

namespace Grumpy.Common.ToBe
{
    internal class AssemblyInfoUtility
    {
        public string Description { get; } 
        public string Title { get; } 
        public string Version { get; } 
        public AssemblyInfoUtility()
        {
            var assembly = Assembly.GetExecutingAssembly();

            Description = GetAssemblyAttribute<AssemblyDescriptionAttribute>(assembly)?.Description ?? "Description not defined in AssemblyInfoUtility.cs";
            Title = GetAssemblyAttribute<AssemblyTitleAttribute>(assembly)?.Title ?? $"MissingTitle.{UniqueKeyUtility.Generate()}";
            Version = GetAssemblyAttribute<AssemblyVersionAttribute>(assembly)?.Version ?? GetAssemblyAttribute<AssemblyFileVersionAttribute>(assembly)?.Version ?? "0.1";
        }

        private static T GetAssemblyAttribute<T>(Assembly assembly) where T : Attribute
        {
            return Attribute.IsDefined(assembly, typeof(T)) ? (T) Attribute.GetCustomAttribute(assembly, typeof(T)) : default(T);
        }
    }
}