BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710060458_UpdateImageFashionMetadata'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ImageFashionMetadata]') AND [c].[name] = N'Category');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [ImageFashionMetadata] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [ImageFashionMetadata] ALTER COLUMN [Category] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710060458_UpdateImageFashionMetadata'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710060458_UpdateImageFashionMetadata', N'10.0.9');
END;

COMMIT;
GO

