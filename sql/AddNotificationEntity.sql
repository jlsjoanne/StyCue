BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731034627_AddNotificationEntity'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [RecipientUserId] int NOT NULL,
        [ActorUserId] int NULL,
        [Type] int NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [ReferenceType] int NOT NULL,
        [ReferenceId] int NULL,
        [IsRead] bit NOT NULL,
        [ReadAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [DeduplicationKey] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Notifications_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731034627_AddNotificationEntity'
)
BEGIN
    CREATE INDEX [IX_Notifications_ActorUserId] ON [Notifications] ([ActorUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731034627_AddNotificationEntity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Notifications_RecipientUserId_DeduplicationKey] ON [Notifications] ([RecipientUserId], [DeduplicationKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731034627_AddNotificationEntity'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientUserId_IsRead_CreatedAt] ON [Notifications] ([RecipientUserId], [IsRead], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731034627_AddNotificationEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731034627_AddNotificationEntity', N'10.0.9');
END;

COMMIT;
GO

