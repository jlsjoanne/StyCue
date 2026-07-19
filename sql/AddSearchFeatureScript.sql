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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719114152_AddFashionDictionary'
)
BEGIN
    CREATE TABLE [FashionSearchDictionaries] (
        [Id] int NOT NULL IDENTITY,
        [CanonicalTerm] nvarchar(100) NOT NULL,
        [Alias] nvarchar(100) NOT NULL,
        [Category] int NOT NULL,
        [Weight] int NOT NULL DEFAULT 1,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_FashionSearchDictionaries] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_FashionSearchDictionaries_Weight_NonNegative] CHECK ([Weight] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719114152_AddFashionDictionary'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FashionSearchDictionaries_CanonicalTerm_Alias] ON [FashionSearchDictionaries] ([CanonicalTerm], [Alias]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719114152_AddFashionDictionary'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719114152_AddFashionDictionary', N'10.0.9');
END;

COMMIT;
GO

