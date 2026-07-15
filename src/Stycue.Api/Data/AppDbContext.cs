using Microsoft.EntityFrameworkCore;
using Stycue.Api.Entities;

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
        }
    }
}
