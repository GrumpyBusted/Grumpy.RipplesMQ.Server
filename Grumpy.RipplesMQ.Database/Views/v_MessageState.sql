CREATE VIEW   [dbo].[v_MessageState]
	AS SELECT *
	     FROM [dbo].[MessageState] [A]
		WHERE [Id] = (SELECT MAX([Id])
		                FROM [dbo].[MessageState] [B]
					   WHERE [B].[MessageId] = [A].[MessageId]
					     AND [B].[SubscriberName] = [A].[SubscriberName])