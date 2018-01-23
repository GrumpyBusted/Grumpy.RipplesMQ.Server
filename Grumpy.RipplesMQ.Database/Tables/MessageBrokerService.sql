CREATE TABLE [dbo].[MessageBrokerService]
(
	[ServerName] NVARCHAR(15) NOT NULL PRIMARY KEY, 
    [ServiceName] NVARCHAR(256) NOT NULL, 
    [InstanceName] NVARCHAR(8) NOT NULL, 
    [RemoteQueueName] NVARCHAR(124) NOT NULL, 
    [LocaleQueueName] NVARCHAR(124) NOT NULL, 
    [LastStartDateTime] DATETIMEOFFSET NOT NULL
)

GO

CREATE UNIQUE INDEX [IX_MessageBrokerService_ServerName] ON [dbo].[MessageBrokerService] ([ServerName], [RemoteQueueName])

GO

CREATE UNIQUE INDEX [IX_MessageBrokerService_ServiceName] ON [dbo].[MessageBrokerService] ([ServiceName], [InstanceName], [ServerName])
