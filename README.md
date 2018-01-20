[![Build status](https://ci.appveyor.com/api/projects/status/j8m99qqh103uqfr9?svg=true)](https://ci.appveyor.com/project/GrumpyBusted/grumpy-ripplesmq-server)
[![codecov](https://codecov.io/gh/GrumpyBusted/Grumpy.RipplesMQ.Server/branch/master/graph/badge.svg)](https://codecov.io/gh/GrumpyBusted/Grumpy.RipplesMQ.Server)

# Grumpy.RipplesMQ.Server
RipplesMQ Message Broker Server.

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
instances of RipplesMQ Server, will cooperate about distributing the work and deligating to other instances 
when needed. This makes RipplesMQ failover save for most parts. 

There is still single point of failure e.g. the common MS-SQL database and MSMQ Locally on a machine.

Features included in the Message Broker:
- Publish/Subscribe (Indirectly Provide/Consume)
- Request/Response (Including asynchronous Request)

For Publish/Subscribe the solution can guaranty delivery to all subscribers, this is only for persistent 
messages. It will not guaranty delivery order and in theory a message could be delivered twice to the 
same subscriber.

The features of RipplesMQ is very similar to the basic features of RabbitMQ, and one of the ideas, when 
building this, was that this was an initial Message Broker that could be replaced with RabbitMQ when the 
use excited the capabilities of RipplesMQ.