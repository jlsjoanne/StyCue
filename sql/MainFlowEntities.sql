BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'AvatarUrl');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Users] DROP COLUMN [AvatarUrl];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    ALTER TABLE [Users] ADD [AvatarImageId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    ALTER TABLE [Users] ADD [DeactivatedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [DailyPointClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ClaimDate] date NOT NULL,
        [Points] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_DailyPointClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DailyPointClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [PointTransactions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Amount] int NOT NULL,
        [TransactionType] int NOT NULL,
        [ReferenceType] int NOT NULL,
        [ReferenceId] int NULL,
        [Description] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PointTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PointTransactions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [Posts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [PostType] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Posts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Posts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [Tags] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [NormalizedName] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Tags] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [UserPointWallets] (
        [UserId] int NOT NULL,
        [CurrentPoints] int NOT NULL,
        [LifetimeEarnedPoints] int NOT NULL,
        [LifetimeSpentPoints] int NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserPointWallets] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_UserPointWallets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [PostFavorites] (
        [PostId] int NOT NULL,
        [UserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PostFavorites] PRIMARY KEY ([PostId], [UserId]),
        CONSTRAINT [FK_PostFavorites_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PostFavorites_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [PostLikes] (
        [PostId] int NOT NULL,
        [UserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PostLikes] PRIMARY KEY ([PostId], [UserId]),
        CONSTRAINT [FK_PostLikes_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PostLikes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [PostTags] (
        [PostId] int NOT NULL,
        [TagId] int NOT NULL,
        CONSTRAINT [PK_PostTags] PRIMARY KEY ([PostId], [TagId]),
        CONSTRAINT [FK_PostTags_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PostTags_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [CommentLikes] (
        [CommentId] int NOT NULL,
        [UserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CommentLikes] PRIMARY KEY ([CommentId], [UserId]),
        CONSTRAINT [FK_CommentLikes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [Comments] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [PostId] int NULL,
        [CommissionId] int NULL,
        [ParentCommentId] int NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Comments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Comments_Comments_ParentCommentId] FOREIGN KEY ([ParentCommentId]) REFERENCES [Comments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Comments_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Comments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [Commissions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [Height] decimal(5,2) NOT NULL,
        [Weight] decimal(5,2) NOT NULL,
        [Age] int NOT NULL,
        [Budget] nvarchar(max) NOT NULL,
        [Points] int NOT NULL,
        [AwardedCommentId] int NULL,
        [AwardedAt] datetime2 NULL,
        [RewardSettledAt] datetime2 NULL,
        [RepostCount] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [ExpiredAt] datetime2 NOT NULL,
        [ClosedAt] datetime2 NULL,
        CONSTRAINT [PK_Commissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Commissions_Comments_AwardedCommentId] FOREIGN KEY ([AwardedCommentId]) REFERENCES [Comments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Commissions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [CommissionFavorites] (
        [CommissionId] int NOT NULL,
        [UserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CommissionFavorites] PRIMARY KEY ([CommissionId], [UserId]),
        CONSTRAINT [FK_CommissionFavorites_Commissions_CommissionId] FOREIGN KEY ([CommissionId]) REFERENCES [Commissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CommissionFavorites_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [CommissionLikes] (
        [CommissionId] int NOT NULL,
        [UserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CommissionLikes] PRIMARY KEY ([CommissionId], [UserId]),
        CONSTRAINT [FK_CommissionLikes_Commissions_CommissionId] FOREIGN KEY ([CommissionId]) REFERENCES [Commissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CommissionLikes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [CommissionReposts] (
        [Id] int NOT NULL IDENTITY,
        [CommissionId] int NOT NULL,
        [UserId] int NOT NULL,
        [SupplementContent] nvarchar(max) NOT NULL,
        [AdditionalPoints] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CommissionReposts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CommissionReposts_Commissions_CommissionId] FOREIGN KEY ([CommissionId]) REFERENCES [Commissions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CommissionReposts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [CommissionTags] (
        [CommissionId] int NOT NULL,
        [TagId] int NOT NULL,
        CONSTRAINT [PK_CommissionTags] PRIMARY KEY ([CommissionId], [TagId]),
        CONSTRAINT [FK_CommissionTags_Commissions_CommissionId] FOREIGN KEY ([CommissionId]) REFERENCES [Commissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CommissionTags_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [ImageAssets] (
        [Id] int NOT NULL IDENTITY,
        [Url] nvarchar(max) NOT NULL,
        [BlobName] nvarchar(max) NOT NULL,
        [ContainerName] nvarchar(max) NOT NULL,
        [ContentType] nvarchar(max) NOT NULL,
        [FileSize] bigint NOT NULL,
        [OwnerUserId] int NOT NULL,
        [Purpose] int NOT NULL,
        [PostId] int NULL,
        [CommissionId] int NULL,
        [CommissionRepostId] int NULL,
        [CommentId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_ImageAssets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ImageAssets_Comments_CommentId] FOREIGN KEY ([CommentId]) REFERENCES [Comments] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ImageAssets_CommissionReposts_CommissionRepostId] FOREIGN KEY ([CommissionRepostId]) REFERENCES [CommissionReposts] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ImageAssets_Commissions_CommissionId] FOREIGN KEY ([CommissionId]) REFERENCES [Commissions] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ImageAssets_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ImageAssets_Users_OwnerUserId] FOREIGN KEY ([OwnerUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE TABLE [ImageFashionMetadata] (
        [ImageAssetId] int NOT NULL,
        [Category] int NOT NULL,
        [Brand] nvarchar(max) NULL,
        CONSTRAINT [PK_ImageFashionMetadata] PRIMARY KEY ([ImageAssetId]),
        CONSTRAINT [FK_ImageFashionMetadata_ImageAssets_ImageAssetId] FOREIGN KEY ([ImageAssetId]) REFERENCES [ImageAssets] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_Users_AvatarImageId] ON [Users] ([AvatarImageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_CommentLikes_UserId] ON [CommentLikes] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_Comments_CommissionId] ON [Comments] ([CommissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_Comments_ParentCommentId] ON [Comments] ([ParentCommentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_Comments_PostId] ON [Comments] ([PostId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_Comments_UserId] ON [Comments] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_CommissionFavorites_UserId] ON [CommissionFavorites] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_CommissionLikes_UserId] ON [CommissionLikes] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CommissionReposts_CommissionId] ON [CommissionReposts] ([CommissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_CommissionReposts_UserId] ON [CommissionReposts] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_Commissions_AwardedCommentId] ON [Commissions] ([AwardedCommentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_Commissions_UserId] ON [Commissions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_CommissionTags_TagId] ON [CommissionTags] ([TagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DailyPointClaims_UserId_ClaimDate] ON [DailyPointClaims] ([UserId], [ClaimDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_ImageAssets_CommentId] ON [ImageAssets] ([CommentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_ImageAssets_CommissionId] ON [ImageAssets] ([CommissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_ImageAssets_CommissionRepostId] ON [ImageAssets] ([CommissionRepostId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_ImageAssets_OwnerUserId] ON [ImageAssets] ([OwnerUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_ImageAssets_PostId] ON [ImageAssets] ([PostId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_PointTransactions_UserId] ON [PointTransactions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_PostFavorites_UserId] ON [PostFavorites] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_PostLikes_UserId] ON [PostLikes] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_Posts_UserId] ON [Posts] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE INDEX [IX_PostTags_TagId] ON [PostTags] ([TagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tags_NormalizedName] ON [Tags] ([NormalizedName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_ImageAssets_AvatarImageId] FOREIGN KEY ([AvatarImageId]) REFERENCES [ImageAssets] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    ALTER TABLE [CommentLikes] ADD CONSTRAINT [FK_CommentLikes_Comments_CommentId] FOREIGN KEY ([CommentId]) REFERENCES [Comments] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    ALTER TABLE [Comments] ADD CONSTRAINT [FK_Comments_Commissions_CommissionId] FOREIGN KEY ([CommissionId]) REFERENCES [Commissions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709101520_MainFlowEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709101520_MainFlowEntities', N'10.0.9');
END;

COMMIT;
GO

