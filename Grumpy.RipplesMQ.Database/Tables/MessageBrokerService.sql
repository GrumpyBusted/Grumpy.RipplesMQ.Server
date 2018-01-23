CREATE TABLE [dbo].[MessageBrokerService]
(
	[ServerName] NVARCHAR(15) NOT NULL , 
    [ServiceName] NVARCHAR(256) NOT NULL, 
    [InstanceName] NVARCHAR(8) NOT NULL, 
    [RemoteQueueName] NVARCHAR(124) NOT NULL, 
    [LocaleQueueName] NVARCHAR(124) NOT NULL, 
    [LastStartDateTime] DATETIMEOFFSET NOT NULL, 
    PRIMARY KEY ([ServerName], [ServiceName], [InstanceName])
)

GO

CREATE UNIQUE INDEX [IX_MessageBrokerService_ServerName] ON [dbo].[MessageBrokerService] ([ServerName], [RemoteQueueName])
