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

