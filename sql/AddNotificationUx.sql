BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802051519_AddNotificationUX'
)
BEGIN
    EXEC sp_rename N'[Notifications].[IX_Notifications_RecipientUserId_DeduplicationKey]', N'UX_Notifications_RecipientUserId_DeduplicationKey', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260802051519_AddNotificationUX'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260802051519_AddNotificationUX', N'10.0.9');
END;

COMMIT;
GO

