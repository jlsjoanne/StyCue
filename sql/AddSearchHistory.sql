BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720060831_AddSearchHistory'
)
BEGIN
    CREATE TABLE [SearchHistories] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Keyword] nvarchar(100) NOT NULL,
        [SearchedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SearchHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SearchHistories_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720060831_AddSearchHistory'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SearchHistories_UserId_Keyword] ON [SearchHistories] ([UserId], [Keyword]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720060831_AddSearchHistory'
)
BEGIN
    CREATE INDEX [IX_SearchHistories_UserId_SearchedAt] ON [SearchHistories] ([UserId], [SearchedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720060831_AddSearchHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260720060831_AddSearchHistory', N'10.0.9');
END;

COMMIT;
GO

