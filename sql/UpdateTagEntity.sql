BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710140808_UpdateTagEntity'
)
BEGIN
    ALTER TABLE [Tags] ADD [TagCategory] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710140808_UpdateTagEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710140808_UpdateTagEntity', N'10.0.9');
END;

COMMIT;
GO

