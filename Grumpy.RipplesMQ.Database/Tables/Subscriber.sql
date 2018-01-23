CREATE TABLE [dbo].[Subscriber]
(
	[Name] NVARCHAR(256) NOT NULL PRIMARY KEY, 
    [Topic] NVARCHAR(256) NOT NULL, 
    [ServerName] NVARCHAR(15) NOT NULL, 
    [ServiceName] NVARCHAR(256) NOT NULL, 
    [InstanceName] NVARCHAR(8) NOT NULL, 
    [QueueName] NVARCHAR(124) NOT NULL, 
    [LastRegisterDateTime] DATETIMEOFFSET NOT NULL
)

        --public string Name { get; set; }
        --public string Topic { get; set; }
        --public string ServerName { get; set; }
        --public string ServiceName { get; set; }
        --public string InstanceName { get; set; }
        --public string QueueName { get; set; }
        --public DateTimeOffset LastRegisterDateTime { get; set; }