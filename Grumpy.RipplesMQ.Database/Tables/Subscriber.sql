CREATE TABLE [dbo].[Subscriber]
(
	[Name] NVARCHAR(256) NOT NULL , 
    [Topic] NVARCHAR(256) NOT NULL, 
    [MessageType] NVARCHAR(512) NOT NULL, 
    [ServerName] NVARCHAR(15) NOT NULL, 
    [ServiceName] NVARCHAR(256) NOT NULL, 
    [QueueName] NVARCHAR(124) NOT NULL, 
    [RegisterDateTime] DATETIMEOFFSET NOT NULL, 
    [PulseDateTime] DATETIMEOFFSET NOT NULL, 
    PRIMARY KEY ([ServerName], [QueueName])
)

GO

