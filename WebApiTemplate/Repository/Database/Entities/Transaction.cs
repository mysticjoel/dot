using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApiTemplate.Repository.Database.Entities
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [ForeignKey(nameof(PaymentAttempt))]
        public int PaymentId { get; set; }

        [Required]
        [MaxLength(50)] // e.g., "Success", "Failed"
        public string Status { get; set; } = default!;

        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation
        public PaymentAttempt PaymentAttempt { get; set; } = default!;
    }
}
