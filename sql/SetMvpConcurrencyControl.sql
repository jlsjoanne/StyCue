BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801113142_SetMvpConcurrencyControl'
)
BEGIN
    ALTER TABLE [Commissions] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801113142_SetMvpConcurrencyControl'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PointTransactions_CommissionSettlement] ON [PointTransactions] ([ReferenceType], [ReferenceId]) WHERE [ReferenceType] = 1 AND [TransactionType] IN (5, 6, 7)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260801113142_SetMvpConcurrencyControl'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260801113142_SetMvpConcurrencyControl', N'10.0.9');
END;

COMMIT;
GO

