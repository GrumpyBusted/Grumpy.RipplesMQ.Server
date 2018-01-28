CREATE VIEW [dbo].[v_Message]
  AS SELECT [MessageState].[MessageId], [MessageState].[SubscriberName], [Message].[Topic], [Message].[Type], [MessageState].[State], [Message].[PublishDateTime], [MessageState].[UpdateDateTime], [Message].[Body]
       FROM [dbo].[MessageState]
            INNER JOIN [dbo].[Message] ON [Message].[Id] = [MessageState].[MessageId] 
      WHERE [MessageState].[Id] = (SELECT MAX([Max].[Id])
                                     FROM [dbo].[MessageState] [Max]
                                    WHERE [Max].[MessageId]      = [MessageState].[MessageId]
                                      AND [Max].[SubscriberName] = [MessageState].[SubscriberName])