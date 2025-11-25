using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApiTemplate.Repository.Database.Entities
{
    public class ExtensionHistory
    {
        [Key]
        public int ExtensionId { get; set; }

        [ForeignKey(nameof(Auction))]
        public int AuctionId { get; set; }

        public DateTime ExtendedAt { get; set; } = DateTime.UtcNow;

        public DateTime PreviousExpiry { get; set; }

        public DateTime NewExpiry { get; set; }

        // Navigation
        public Auction Auction { get; set; } = default!;
    }
}
