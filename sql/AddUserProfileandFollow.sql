BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715133933_AddUserProfileandFollow'
)
BEGIN
    CREATE TABLE [UserFollows] (
        [FollowerUserId] int NOT NULL,
        [FollowingUserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserFollows] PRIMARY KEY ([FollowerUserId], [FollowingUserId]),
        CONSTRAINT [CK_UserFollows_NotSelf] CHECK ([FollowerUserId] <> [FollowingUserId]),
        CONSTRAINT [FK_UserFollows_Users_FollowerUserId] FOREIGN KEY ([FollowerUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserFollows_Users_FollowingUserId] FOREIGN KEY ([FollowingUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715133933_AddUserProfileandFollow'
)
BEGIN
    CREATE TABLE [UserProfiles] (
        [UserId] int NOT NULL,
        [Height] decimal(5,2) NULL,
        [Weight] decimal(5,2) NULL,
        [BirthDate] datetime2 NULL,
        [Bio] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_UserProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715133933_AddUserProfileandFollow'
)
BEGIN
    CREATE INDEX [IX_UserFollows_FollowingUserId] ON [UserFollows] ([FollowingUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715133933_AddUserProfileandFollow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260715133933_AddUserProfileandFollow', N'10.0.9');
END;

COMMIT;
GO

