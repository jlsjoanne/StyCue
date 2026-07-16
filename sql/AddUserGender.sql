BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716024422_AddUserGender'
)
BEGIN
    ALTER TABLE [UserProfiles] ADD [Gender] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716024422_AddUserGender'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716024422_AddUserGender', N'10.0.9');
END;

COMMIT;
GO

