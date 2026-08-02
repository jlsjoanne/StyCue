DROP FULLTEXT INDEX ON [dbo].[SearchDocuments];
GO

DROP FULLTEXT CATALOG [StycueSearchCatalog];
GO

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20260720092219_AddSearchDocumentFullTextIndex';
GO

