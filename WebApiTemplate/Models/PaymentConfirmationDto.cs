using System.ComponentModel.DataAnnotations;

namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for payment confirmation request
    /// </summary>
    public class PaymentConfirmationDto
    {
        /// <summary>
        /// Product ID for the auction
        /// </summary>
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// Amount confirmed by the bidder
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Confirmed amount must be greater than 0")]
        public decimal ConfirmedAmount { get; set; }
    }
}

