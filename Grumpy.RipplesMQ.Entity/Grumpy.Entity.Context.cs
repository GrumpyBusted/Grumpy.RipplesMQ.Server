



using Grumpy.Entity.Interfaces;

namespace Grumpy.RipplesMQ.Entity
{

public partial class Entities
{
    public Entities(IEntityConnectionConfig entityConnectionConfig) : base(entityConnectionConfig.ConnectionString("Grumpy.RipplesMQ.Entity", "Model"))
    {
#pragma warning disable S1481
        // NOTE: Using type from EntityFramework.SqlServer to ensure copy of dll to all application using this dll
        // ReSharper disable once UnusedVariable
        var instance = System.Data.Entity.SqlServer.SqlProviderServices.Instance;
#pragma warning restore S1481
    }
}

}
