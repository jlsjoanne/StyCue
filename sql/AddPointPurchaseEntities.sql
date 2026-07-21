BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717080159_AddPointPurchaseEntities'
)
BEGIN
    CREATE TABLE [PointProducts] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [PriceTwd] int NOT NULL,
        [Points] int NOT NULL,
        [BasePoints] int NOT NULL,
        [BonusPoints] int NOT NULL,
        [IsActive] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_PointProducts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PointProducts_Points_Valid] CHECK ([BasePoints] >= 0 AND [BonusPoints] >= 0 AND [Points] > 0 AND [Points] = [BasePoints] + [BonusPoints]),
        CONSTRAINT [CK_PointProducts_PriceTwd_Positive] CHECK ([PriceTwd] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717080159_AddPointPurchaseEntities'
)
BEGIN
    CREATE TABLE [PointPurchaseOrders] (
        [Id] int NOT NULL IDENTITY,
        [MerchantTradeNo] nvarchar(20) NOT NULL,
        [UserId] int NOT NULL,
        [PointProductId] int NOT NULL,
        [AmountTwd] int NOT NULL,
        [Points] int NOT NULL,
        [PaymentProvider] nvarchar(20) NOT NULL DEFAULT N'Ecpay',
        [PaymentMethod] nvarchar(20) NOT NULL DEFAULT N'CreditCard',
        [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
        [ProviderTradeNo] nvarchar(64) NULL,
        [PaidAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PointPurchaseOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PointPurchaseOrders_AmountTwd_Positive] CHECK ([AmountTwd] > 0),
        CONSTRAINT [CK_PointPurchaseOrders_Points_Positive] CHECK ([Points] > 0),
        CONSTRAINT [FK_PointPurchaseOrders_PointProducts_PointProductId] FOREIGN KEY ([PointProductId]) REFERENCES [PointProducts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PointPurchaseOrders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717080159_AddPointPurchaseEntities'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BasePoints', N'BonusPoints', N'Code', N'DisplayOrder', N'IsActive', N'Name', N'Points', N'PriceTwd') AND [object_id] = OBJECT_ID(N'[PointProducts]'))
        SET IDENTITY_INSERT [PointProducts] ON;
    EXEC(N'INSERT INTO [PointProducts] ([Id], [BasePoints], [BonusPoints], [Code], [DisplayOrder], [IsActive], [Name], [Points], [PriceTwd])
    VALUES (1, 100, 0, N''POINT_100'', 1, CAST(1 AS bit), N''基礎點數方案'', 100, 49),
    (2, 200, 50, N''POINT_250'', 2, CAST(1 AS bit), N''超值點數方案'', 250, 99),
    (3, 400, 100, N''POINT_500'', 3, CAST(1 AS bit), N''熱門點數方案'', 500, 199),
    (4, 600, 150, N''POINT_750'', 4, CAST(1 AS bit), N''大容量點數方案'', 750, 299)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BasePoints', N'BonusPoints', N'Code', N'DisplayOrder', N'IsActive', N'Name', N'Points', N'PriceTwd') AND [object_id] = OBJECT_ID(N'[PointProducts]'))
        SET IDENTITY_INSERT [PointProducts] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717080159_AddPointPurchaseEntities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PointProducts_Code] ON [PointProducts] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717080159_AddPointPurchaseEntities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PointPurchaseOrders_MerchantTradeNo] ON [PointPurchaseOrders] ([MerchantTradeNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717080159_AddPointPurchaseEntities'
)
BEGIN
    CREATE INDEX [IX_PointPurchaseOrders_PointProductId] ON [PointPurchaseOrders] ([PointProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717080159_AddPointPurchaseEntities'
)
BEGIN
    CREATE INDEX [IX_PointPurchaseOrders_UserId] ON [PointPurchaseOrders] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717080159_AddPointPurchaseEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260717080159_AddPointPurchaseEntities', N'10.0.9');
END;

COMMIT;
GO

