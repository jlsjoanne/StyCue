BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719062912_AddSearchDocument'
)
BEGIN
    CREATE TABLE [SearchDocuments] (
        [Id] nvarchar(64) NOT NULL,
        [ItemType] int NOT NULL,
        [ItemId] int NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [TagsText] nvarchar(max) NOT NULL,
        [SearchText] nvarchar(max) NOT NULL,
        [IsVisible] bit NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SearchDocuments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719062912_AddSearchDocument'
)
BEGIN
    CREATE INDEX [IX_SearchDocuments_IsVisible_UpdatedAt] ON [SearchDocuments] ([IsVisible], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719062912_AddSearchDocument'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SearchDocuments_ItemType_ItemId] ON [SearchDocuments] ([ItemType], [ItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719062912_AddSearchDocument'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719062912_AddSearchDocument', N'10.0.9');
END;

COMMIT;
GO

