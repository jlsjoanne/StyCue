IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720092219_AddSearchDocumentFullTextIndex'
)
BEGIN
    CREATE FULLTEXT CATALOG [StycueSearchCatalog];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720092219_AddSearchDocumentFullTextIndex'
)
BEGIN
    CREATE FULLTEXT INDEX ON [dbo].[SearchDocuments]
    (
        [SearchText] LANGUAGE 1028
    )
    KEY INDEX [PK_SearchDocuments]
    ON [StycueSearchCatalog]
    WITH CHANGE_TRACKING AUTO;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720092219_AddSearchDocumentFullTextIndex'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260720092219_AddSearchDocumentFullTextIndex', N'10.0.9');
END;
GO

