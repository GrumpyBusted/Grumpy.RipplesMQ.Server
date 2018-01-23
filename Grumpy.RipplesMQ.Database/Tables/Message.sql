CREATE TABLE [dbo].[Message]
(
	[Id] NVARCHAR(64) NOT NULL PRIMARY KEY, 
    [Topic] NVARCHAR(256) NOT NULL, 
    [Type] NVARCHAR(2014) NOT NULL, 
    [Body] NVARCHAR(MAX) NOT NULL, 
    [PublishDateTime] DATETIMEOFFSET NOT NULL
)
GO

CREATE INDEX [IX_Message_Topic] ON [dbo].[Message] ([Topic])
