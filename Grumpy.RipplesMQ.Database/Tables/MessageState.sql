CREATE TABLE [dbo].[MessageState]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1, 1), 
	[MessageId] NVARCHAR(64) NOT NULL, 
    [SubscriberName] NVARCHAR(256) NOT NULL, 
    [State] NVARCHAR(32) NOT NULL, 
    [ErrorCount] INT NOT NULL, 
    [UpdateDateTime] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE UNIQUE INDEX [IX_MessageState_MessageId] ON [dbo].[MessageState] ([MessageId], [SubscriberName], [UpdateDateTime] DESC, [Id] DESC)
GO

CREATE INDEX [IX_MessageState_SubscriberName] ON [dbo].[MessageState] ([SubscriberName])
