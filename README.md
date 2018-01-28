[![Build status](https://ci.appveyor.com/api/projects/status/j8m99qqh103uqfr9?svg=true)](https://ci.appveyor.com/project/GrumpyBusted/grumpy-ripplesmq-server)
[![codecov](https://codecov.io/gh/GrumpyBusted/Grumpy.RipplesMQ.Server/branch/master/graph/badge.svg)](https://codecov.io/gh/GrumpyBusted/Grumpy.RipplesMQ.Server)
[![nuget](https://img.shields.io/nuget/v/Grumpy.RipplesMQ.Server.svg)](https://www.nuget.org/packages/Grumpy.RipplesMQ.Server/)
[![downloads](https://img.shields.io/nuget/dt/Grumpy.RipplesMQ.Server.svg)](https://www.nuget.org/packages/Grumpy.RipplesMQ.Server/)

# Grumpy.RipplesMQ.Server
RipplesMQ is a simple Message Broker, this library contain the server part, use
[Grumpy.RipplesMQ.Client](https://github.com/GrumpyBusted/Grumpy.RipplesMQ.Client) to build services 
using RipplesMQ Server.

The goal of the project was to develop an Open Source Message Broker using standard Windows features 
as infrastructure. The solution uses the Microsoft Message Queue (MSMQ) for communication and Microsoft 
SQL Server (MS-SQL) as persistent layer. It might seem strange to use these components as infrastructure 
for a Message Broker, but the reason is that many organizations don't want extra infrastructure. It might 
be difficult for developers to convince management and infrastructure teams to add RabbitMQ, Kafka etc. 
to the infrastructure stack. But with this solution the developer can download, install and use a Message 
Broker with-out being stopped by the organization.

Secondary use could be for the developer that wants to setup a few Micro Services at home but done want 
the extra infrastructure of and other messaging system.

RipplesMQ can run in multiple instances on the same machine and on different machines, the different 
instances of RipplesMQ Server, will cooperate on distributing the work and deligating to other instances 
when needed. This makes RipplesMQ failover save for most parts. 

There is still single point of failure e.g. the common MS-SQL database and MSMQ Locally on a machine.

Features included in the Message Broker:
- Publish/Subscribe (Indirectly Provide/Consume)
- Request/Response (Including asynchronous Request)

For Publish/Subscribe the solution can guaranty delivery to all subscribers, this is only for persistent 
messages. It will not guaranty delivery order and in theory a message could be delivered twice to the 
same subscriber. By using named subscribers you can active a Provide/Consume pattern as one subscriber 
with a given name will receive the published message.

The features of RipplesMQ is very similar to the basic features of RabbitMQ, and one of the ideas, when 
building this, was that this was an initial Message Broker that could be replaced with RabbitMQ when the 
use excited the capabilities of RipplesMQ.

RipplesMQ Message broker will normally use a MS-SQL Database for storing persistent published messages
and keep track of their state on the individual subscribers. It will also use the database for identifying
other instances of the RipplesMQ Message Broker Server. If there is no database connection string in the 
configuration for the Message Broker, the server will run without database as a standalone server,
persistent messages will only be in MSMQ and the server cannot find any counterparts to cooperate with.
This could work in a test scenario, but is not recommended for production use.

RipplesMQ Message Broker Server is published as a NuGet packages and the intent is that you implemented
your own service. In the contents folder of the Nuget packages you will find sql and dacpac file for
deploying the database. Alternativly get the source of my implementation of the service at 
[Grumpy.RipplesMQ.Sample](https://github.com/GrumpyBusted/Grumpy.RipplesMQ.Sample).

```csharp
// Configuration for RipplesMQ Server, ServiceName will be defaulted to the process name. As
// mentioned you can even run without database settings.
var messageBrokerServiceConfig = new MessageBrokerServiceConfig
{
    ServiceName = "RipplesMQ.Server",
    DatabaseServer = "(localdb)\MSSQLLocalDB",
    DatabaseName = "RipplesMQ",
};
 
// Constructing the RipplesMQ Message Broker
using (var messageBroker = MessageBrokerBuilder.Build(messageBrokerServiceConfig))
{
    // Start the Message Broker
    messageBroker.Start(cancellationToken);

    // Wait forever :-)

    // Stop the Message Broker
    messageBroker.Stop();
}
```