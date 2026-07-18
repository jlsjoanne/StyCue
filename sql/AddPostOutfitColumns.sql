BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718022451_AddPostOutfitColumns'
)
BEGIN
    ALTER TABLE [Posts] ADD [OutfitDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718022451_AddPostOutfitColumns'
)
BEGIN
    ALTER TABLE [Posts] ADD [OutfitLocation] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718022451_AddPostOutfitColumns'
)
BEGIN
    ALTER TABLE [Posts] ADD [OutfitOccasion] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718022451_AddPostOutfitColumns'
)
BEGIN
    ALTER TABLE [Posts] ADD [OutfitStyle] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718022451_AddPostOutfitColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260718022451_AddPostOutfitColumns', N'10.0.9');
END;

COMMIT;
GO

