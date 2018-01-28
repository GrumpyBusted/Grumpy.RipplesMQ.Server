CREATE TABLE [dbo].[MessageBrokerService]
(
	[ServerName] NVARCHAR(15) NOT NULL , 
    [ServiceName] NVARCHAR(256) NOT NULL, 
    [RemoteQueueName] NVARCHAR(124) NOT NULL, 
    [LocaleQueueName] NVARCHAR(124) NOT NULL, 
    [StartDateTime] DATETIMEOFFSET NOT NULL, 
    [PulseDateTime] DATETIMEOFFSET NOT NULL, 
    PRIMARY KEY ([ServerName], [ServiceName])
)

GO

CREATE UNIQUE INDEX [IX_MessageBrokerService_ServerName] ON [dbo].[MessageBrokerService] ([ServerName], [RemoteQueueName])
