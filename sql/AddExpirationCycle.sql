BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801120810_AddExpirationCycle'
)
BEGIN
    ALTER TABLE [Commissions] ADD [ExpirationCycle] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801120810_AddExpirationCycle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260801120810_AddExpirationCycle', N'10.0.9');
END;

COMMIT;
GO

