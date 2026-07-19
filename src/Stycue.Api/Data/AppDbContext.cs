using Microsoft.EntityFrameworkCore;
using Stycue.Api.Entities;
using Stycue.Api.Enums;

namespace Stycue.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Commission> Commissions => Set<Commission>();
        public DbSet<CommissionRepost> CommissionReposts => Set<CommissionRepost>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<ImageAsset> ImageAssets => Set<ImageAsset>();
        public DbSet<ImageFashionMetadata> ImageFashionMetadata => Set<ImageFashionMetadata>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<PostTag> PostTags => Set<PostTag>();
        public DbSet<CommissionTag> CommissionTags => Set<CommissionTag>();
        public DbSet<PostLike> PostLikes => Set<PostLike>();
        public DbSet<CommissionLike> CommissionLikes => Set<CommissionLike>();
        public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
        public DbSet<PostFavorite> PostFavorites => Set<PostFavorite>();
        public DbSet<CommissionFavorite> CommissionFavorites => Set<CommissionFavorite>();
        public DbSet<UserPointWallet> UserPointWallets => Set<UserPointWallet>();
        public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
        public DbSet<DailyPointClaim> DailyPointClaims => Set<DailyPointClaim>();

        public DbSet<UserFollow> UserFollows => Set<UserFollow>();

        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<PointProduct> PointProducts => Set<PointProduct>();
        public DbSet<PointPurchaseOrder> PointPurchaseOrders => Set<PointPurchaseOrder>();

        public DbSet<SearchDocument> SearchDocuments => Set<SearchDocument>();

        public DbSet<FashionSearchDictionary> FashionSearchDictionaries=> Set<FashionSearchDictionary>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();

                entity.HasIndex(u => u.GoogleSub)
                    .IsUnique()
                    .HasFilter("[GoogleSub] IS NOT NULL");

                entity.HasOne(u => u.AvatarImage)
                    .WithMany()
                    .HasForeignKey(u => u.AvatarImageId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Commission>(entity =>
            {
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.Height)
                    .HasPrecision(5, 2);

                entity.Property(x => x.Weight)
                    .HasPrecision(5, 2);

                entity.HasOne(x => x.AwardedComment)
                    .WithMany()
                    .HasForeignKey(x => x.AwardedCommentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CommissionRepost>(entity =>
            {
                entity.HasIndex(x => x.CommissionId)
                    .IsUnique();

                entity.HasOne(x => x.Commission)
                    .WithMany(x => x.Reposts)
                    .HasForeignKey(x => x.CommissionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Post)
                    .WithMany(x => x.Comments)
                    .HasForeignKey(x => x.PostId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Commission)
                    .WithMany(x => x.Comments)
                    .HasForeignKey(x => x.CommissionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ParentComment)
                    .WithMany(x => x.Replies)
                    .HasForeignKey(x => x.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ImageAsset>(entity =>
            {
                entity.HasOne(x => x.OwnerUser)
                    .WithMany()
                    .HasForeignKey(x => x.OwnerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Post)
                    .WithMany(x => x.Images)
                    .HasForeignKey(x => x.PostId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.Commission)
                    .WithMany(x => x.Images)
                    .HasForeignKey(x => x.CommissionId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.Comment)
                    .WithMany(x => x.Images)
                    .HasForeignKey(x => x.CommentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.CommissionRepost)
                    .WithMany(x => x.Images)
                    .HasForeignKey(x => x.CommissionRepostId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ImageFashionMetadata>(entity =>
            {
                entity.HasKey(x => x.ImageAssetId);

                entity.HasOne(x => x.ImageAsset)
                    .WithOne(x => x.FashionMetadata)
                    .HasForeignKey<ImageFashionMetadata>(x => x.ImageAssetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasIndex(x => x.NormalizedName)
                    .IsUnique();
            });

            modelBuilder.Entity<PostTag>(entity =>
            {
                entity.HasKey(x => new { x.PostId, x.TagId });
            });

            modelBuilder.Entity<CommissionTag>(entity =>
            {
                entity.HasKey(x => new { x.CommissionId, x.TagId });
            });

            modelBuilder.Entity<PostLike>(entity =>
            {
                entity.HasKey(x => new { x.PostId, x.UserId });

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CommissionLike>(entity =>
            {
                entity.HasKey(x => new { x.CommissionId, x.UserId });

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CommentLike>(entity =>
            {
                entity.HasKey(x => new { x.CommentId, x.UserId });

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PostFavorite>(entity =>
            {
                entity.HasKey(x => new { x.PostId, x.UserId });

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CommissionFavorite>(entity =>
            {
                entity.HasKey(x => new { x.CommissionId, x.UserId });

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserPointWallet>(entity =>
            {
                entity.HasKey(x => x.UserId);

                entity.HasOne(x => x.User)
                    .WithOne()
                    .HasForeignKey<UserPointWallet>(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PointTransaction>(entity =>
            {
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DailyPointClaim>(entity =>
            {
                entity.HasIndex(x => new { x.UserId, x.ClaimDate })
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserFollow>(entity =>
            {
                entity.HasKey(x => new { x.FollowerUserId, x.FollowingUserId });
                
                entity.HasIndex(x => x.FollowingUserId);

                entity.HasOne(x => x.FollowerUser)
                    .WithMany()
                    .HasForeignKey(x => x.FollowerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.FollowingUser)
                    .WithMany()
                    .HasForeignKey(x => x.FollowingUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t => t.HasCheckConstraint("CK_UserFollows_NotSelf", "[FollowerUserId] <> [FollowingUserId]"));
            });

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(x => x.UserId);

                entity.HasOne(x => x.User)
                    .WithOne(x => x.Profile)
                    .HasForeignKey<UserProfile>(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.Height).HasPrecision(5, 2);
                entity.Property(x => x.Weight).HasPrecision(5, 2);

                entity.Property(x => x.Gender)
                    .HasConversion<int>();
            });

            modelBuilder.Entity<PointProduct>(entity =>
            {
                entity.HasIndex(x => x.Code).IsUnique();

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_PointProducts_PriceTwd_Positive", "[PriceTwd] > 0");
                    t.HasCheckConstraint(
                        "CK_PointProducts_Points_Valid", 
                        "[BasePoints] >= 0 AND [BonusPoints] >= 0 AND [Points] > 0 AND [Points] = [BasePoints] + [BonusPoints]");
                });

                entity.HasData(
                    new PointProduct
                    {
                      Id = 1,
                      Code = "POINT_100",
                      Name = "基礎點數方案",
                      PriceTwd = 49,
                      BasePoints = 100,
                      BonusPoints = 0,
                      Points = 100,
                      IsActive = true,
                      DisplayOrder = 1
                  },
                  new PointProduct
                  {
                      Id = 2,
                      Code = "POINT_250",
                      Name = "超值點數方案",
                      PriceTwd = 99,
                      BasePoints = 200,
                      BonusPoints = 50,
                      Points = 250,
                      IsActive = true,
                      DisplayOrder = 2
                  },
                  new PointProduct
                  {
                      Id = 3,
                      Code = "POINT_500",
                      Name = "熱門點數方案",
                      PriceTwd = 199,
                      BasePoints = 400,
                      BonusPoints = 100,
                      Points = 500,
                      IsActive = true,
                      DisplayOrder = 3
                  },
                  new PointProduct
                  {
                      Id = 4,
                      Code = "POINT_750",
                      Name = "大容量點數方案",
                      PriceTwd = 299,
                      BasePoints = 600,
                      BonusPoints = 150,
                      Points = 750,
                      IsActive = true,
                      DisplayOrder = 4
                  });
            });

            modelBuilder.Entity<PointPurchaseOrder>(entity =>
            {
                entity.HasIndex(x => x.MerchantTradeNo).IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.PointProduct)
                    .WithMany()
                    .HasForeignKey(x => x.PointProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_PointPurchaseOrders_AmountTwd_Positive", "[AmountTwd] > 0");
                    t.HasCheckConstraint(
                        "CK_PointPurchaseOrders_Points_Positive", "[Points] > 0");
                });

                entity.Property(x => x.PaymentProvider)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .HasDefaultValue(PaymentProvider.Ecpay);

                entity.Property(x => x.PaymentMethod)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .HasDefaultValue(PaymentMethod.CreditCard);

                entity.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .HasDefaultValue(PointPurchaseStatus.Pending);
            });

            modelBuilder.Entity<SearchDocument>(entity =>
            {
                entity.Property(x => x.Id).HasMaxLength(64).ValueGeneratedNever();
                entity.HasIndex(x => new { x.ItemType, x.ItemId }).IsUnique();
                entity.HasIndex(x => new { x.IsVisible, x.UpdatedAt });
            });

            modelBuilder.Entity<FashionSearchDictionary>(entity =>
            {
                entity.HasIndex(x => new { x.CanonicalTerm, x.Alias }).IsUnique();
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_FashionSearchDictionaries_Weight_NonNegative",
                    "[Weight] >= 0"));
                entity.Property(x => x.Weight).HasDefaultValue(1);

                entity.Property(x => x.IsActive).HasDefaultValue(true);
            });
        }
    }
}
