#region References
using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Repository.Database.Entities;
#endregion

namespace WebApiTemplate.Repository.Database
{
    public class WenApiTemplateDbContext : DbContext
    {
        public WenApiTemplateDbContext(DbContextOptions<WenApiTemplateDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Product> Products { get; set; } = default!;
        public DbSet<Auction> Auctions { get; set; } = default!;
        public DbSet<Bid> Bids { get; set; } = default!;
        public DbSet<PaymentAttempt> PaymentAttempts { get; set; } = default!;
        public DbSet<ExtensionHistory> ExtensionHistories { get; set; } = default!;
        public DbSet<Transaction> Transactions { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(x => x.Email).IsUnique();
                e.HasIndex(x => x.Role);
            });

            // Product
            modelBuilder.Entity<Product>(e =>
            {
                e.HasIndex(x => x.Category);
                e.HasIndex(x => x.OwnerId);

                // Product -> Owner (User) many-to-one
                e.HasOne(p => p.Owner)
                 .WithMany(u => u.ProductsOwned)
                 .HasForeignKey(p => p.OwnerId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Product -> HighestBid (optional) one-to-one-like via FK
                e.HasOne(p => p.HighestBid)
                 .WithMany() // no back-collection to keep it simple
                 .HasForeignKey(p => p.HighestBidId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Auction
            modelBuilder.Entity<Auction>(e =>
            {
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.ProductId).IsUnique(); // One Product -> One Auction

                // Auction -> Product (one-to-one)
                e.HasOne(a => a.Product)
                 .WithOne(p => p.Auction)
                 .HasForeignKey<Auction>(a => a.ProductId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Auction -> HighestBid (optional)
                e.HasOne(a => a.HighestBid)
                 .WithMany()
                 .HasForeignKey(a => a.HighestBidId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Bid
            modelBuilder.Entity<Bid>(e =>
            {
                e.HasIndex(x => new { x.AuctionId, x.Timestamp });
                e.HasIndex(x => x.BidderId);

                e.HasOne(b => b.Auction)
                 .WithMany() // if you later add Auction.Bids, change to . WithMany(a => a.Bids)
                 .HasForeignKey(b => b.AuctionId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(b => b.Bidder)
                 .WithMany(u => u.Bids)
                 .HasForeignKey(b => b.BidderId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // PaymentAttempt
            modelBuilder.Entity<PaymentAttempt>(e =>
            {
                e.HasIndex(x => x.AuctionId);
                e.HasIndex(x => x.BidderId);
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.AttemptTime);

                e.HasOne(pa => pa.Auction)
                 .WithMany() // if you later add Auction.PaymentAttempts, switch to collection
                 .HasForeignKey(pa => pa.AuctionId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(pa => pa.Bidder)
                 .WithMany() // if you later add User.PaymentAttempts, switch to collection
                 .HasForeignKey(pa => pa.BidderId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ExtensionHistory
            modelBuilder.Entity<ExtensionHistory>(e =>
            {
                e.HasIndex(x => x.AuctionId);
                e.HasIndex(x => x.ExtendedAt);

                e.HasOne(ex => ex.Auction)
                 .WithMany() // if you later add Auction.ExtensionHistories, switch to collection
                 .HasForeignKey(ex => ex.AuctionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Transaction
            modelBuilder.Entity<Transaction>(e =>
            {
                e.HasIndex(x => x.PaymentId);
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.Timestamp);

                e.HasOne(t => t.PaymentAttempt)
                 .WithMany() // if you later add PaymentAttempt.Transactions, switch to collection
                 .HasForeignKey(t => t.PaymentId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
