using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApiTemplate.Configuration;
using WebApiTemplate.Constants;
using WebApiTemplate.Exceptions;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Repository.DatabaseOperation.Interface;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Service
{
    /// <summary>
    /// Service for handling payment processing and confirmation
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentOperation _paymentOperation;
        private readonly IBidOperation _bidOperation;
        private readonly IEmailService _emailService;
        private readonly PaymentSettings _paymentSettings;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentOperation paymentOperation,
            IBidOperation bidOperation,
            IEmailService emailService,
            IOptions<PaymentSettings> paymentSettings,
            ILogger<PaymentService> logger)
        {
            _paymentOperation = paymentOperation;
            _bidOperation = bidOperation;
            _emailService = emailService;
            _paymentSettings = paymentSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Creates the first payment attempt for an auction when it expires
        /// </summary>
        public async Task<PaymentAttempt> CreateFirstPaymentAttemptAsync(int auctionId)
        {
            _logger.LogInformation("Creating first payment attempt for auction {AuctionId}", auctionId);

            var auction = await _bidOperation.GetAuctionByIdAsync(auctionId);
            if (auction == null)
            {
                throw new KeyNotFoundException($"Auction {auctionId} not found");
            }

            if (!auction.HighestBidId.HasValue || auction.HighestBid == null)
            {
                throw new InvalidOperationException($"Auction {auctionId} has no bids");
            }

            var highestBid = auction.HighestBid;
            var now = DateTime.UtcNow;

            var paymentAttempt = new PaymentAttempt
            {
                AuctionId = auctionId,
                BidderId = highestBid.BidderId,
                Status = PaymentStatus.Pending,
                AttemptNumber = 1,
                AttemptTime = now,
                ExpiryTime = now.AddMinutes(_paymentSettings.WindowMinutes),
                Amount = highestBid.Amount
            };

            var createdAttempt = await _paymentOperation.CreatePaymentAttemptAsync(paymentAttempt);

            _logger.LogInformation(
                "Created payment attempt {PaymentId} for auction {AuctionId}, bidder {BidderId}, amount {Amount}, expires {ExpiryTime}",
                createdAttempt.PaymentId, auctionId, highestBid.BidderId, highestBid.Amount, createdAttempt.ExpiryTime);

            // Send email notification
            await _emailService.SendPaymentNotificationAsync(
                createdAttempt.Bidder,
                createdAttempt.Auction,
                createdAttempt);

            return createdAttempt;
        }

        /// <summary>
        /// Confirms a payment for a product/auction
        /// </summary>
        public async Task<Transaction> ConfirmPaymentAsync(
            int productId,
            int userId,
            decimal confirmedAmount,
            bool testInstantFail)
        {
            _logger.LogInformation(
                "Processing payment confirmation for product {ProductId}, user {UserId}, amount {Amount}, testInstantFail {TestInstantFail}",
                productId, userId, confirmedAmount, testInstantFail);

            // Get auction by product ID
            var auction = await _bidOperation.GetAuctionByIdAsync(productId);
            if (auction == null || auction.ProductId != productId)
            {
                // Find auction by product ID
                var auctions = await _paymentOperation.GetBidsByAuctionOrderedAsync(productId);
                if (auctions.Count > 0)
                {
                    auction = auctions.First().Auction;
                }
            }

            if (auction == null)
            {
                throw new KeyNotFoundException($"No auction found for product {productId}");
            }

            // Get current payment attempt
            var paymentAttempt = await _paymentOperation.GetCurrentPaymentAttemptAsync(auction.AuctionId);
            if (paymentAttempt == null)
            {
                throw new KeyNotFoundException($"No active payment attempt found for auction {auction.AuctionId}");
            }

            // Validate user is the current eligible winner
            if (paymentAttempt.BidderId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to confirm payment but bidder is {BidderId}",
                    userId, paymentAttempt.BidderId);
                throw new UnauthorizedPaymentException(userId, paymentAttempt.BidderId);
            }

            // Check if test instant fail is enabled
            if (testInstantFail)
            {
                _logger.LogInformation("Test instant fail enabled - marking payment as failed immediately");
                return await HandlePaymentFailure(paymentAttempt, confirmedAmount, "Test instant fail triggered");
            }

            // Check if payment window has expired
            if (DateTime.UtcNow > paymentAttempt.ExpiryTime)
            {
                _logger.LogWarning(
                    "Payment window expired for payment attempt {PaymentId}, expiry {ExpiryTime}",
                    paymentAttempt.PaymentId, paymentAttempt.ExpiryTime);
                throw new PaymentWindowExpiredException(paymentAttempt.ExpiryTime);
            }

            // Validate amount matches highest bid
            if (confirmedAmount != paymentAttempt.Amount)
            {
                _logger.LogWarning(
                    "Payment amount mismatch for payment attempt {PaymentId}. Expected {Expected}, got {Confirmed}",
                    paymentAttempt.PaymentId, paymentAttempt.Amount, confirmedAmount);
                
                return await HandlePaymentFailure(
                    paymentAttempt,
                    confirmedAmount,
                    $"Amount mismatch. Expected: {paymentAttempt.Amount:C}, Confirmed: {confirmedAmount:C}");
            }

            // Payment successful
            _logger.LogInformation("Payment confirmed successfully for payment attempt {PaymentId}", paymentAttempt.PaymentId);

            // Update payment attempt
            paymentAttempt.Status = PaymentStatus.Success;
            paymentAttempt.ConfirmedAmount = confirmedAmount;
            await _paymentOperation.UpdatePaymentAttemptAsync(paymentAttempt);

            // Update auction status to completed
            auction.Status = AuctionStatus.Completed;
            await _bidOperation.UpdateAuctionAsync(auction);

            // Create success transaction
            var transaction = new Transaction
            {
                PaymentId = paymentAttempt.PaymentId,
                Status = TransactionStatus.Success,
                Amount = confirmedAmount,
                Timestamp = DateTime.UtcNow
            };

            var createdTransaction = await _paymentOperation.CreateTransactionAsync(transaction);

            _logger.LogInformation(
                "Transaction {TransactionId} created successfully for payment {PaymentId}",
                createdTransaction.TransactionId, paymentAttempt.PaymentId);

            return createdTransaction;
        }

        /// <summary>
        /// Gets all expired payment attempts
        /// </summary>
        public async Task<List<PaymentAttempt>> GetExpiredPaymentAttemptsAsync()
        {
            return await _paymentOperation.GetExpiredPaymentAttemptsAsync();
        }

        /// <summary>
        /// Processes a failed payment attempt and triggers retry logic
        /// </summary>
        public async Task ProcessFailedPaymentAsync(int paymentId)
        {
            _logger.LogInformation("Processing failed payment for payment attempt {PaymentId}", paymentId);

            var paymentAttempt = await _paymentOperation.GetPaymentAttemptByIdAsync(paymentId);
            if (paymentAttempt == null)
            {
                _logger.LogWarning("Payment attempt {PaymentId} not found", paymentId);
                return;
            }

            // Mark current attempt as failed if still pending
            if (paymentAttempt.Status == PaymentStatus.Pending)
            {
                paymentAttempt.Status = PaymentStatus.Failed;
                await _paymentOperation.UpdatePaymentAttemptAsync(paymentAttempt);

                // Create failed transaction record
                var failedTransaction = new Transaction
                {
                    PaymentId = paymentAttempt.PaymentId,
                    Status = TransactionStatus.Failed,
                    Amount = paymentAttempt.Amount ?? 0,
                    Timestamp = DateTime.UtcNow
                };
                await _paymentOperation.CreateTransactionAsync(failedTransaction);
            }

            // Check total attempt count
            var attemptCount = await _paymentOperation.GetPaymentAttemptCountAsync(paymentAttempt.AuctionId);
            
            _logger.LogInformation(
                "Auction {AuctionId} has {AttemptCount} payment attempts (max: {MaxAttempts})",
                paymentAttempt.AuctionId, attemptCount, _paymentSettings.MaxRetryAttempts);

            if (attemptCount >= _paymentSettings.MaxRetryAttempts)
            {
                // Max attempts reached - mark auction as failed
                _logger.LogWarning(
                    "Max payment attempts ({MaxAttempts}) reached for auction {AuctionId}, marking as failed",
                    _paymentSettings.MaxRetryAttempts, paymentAttempt.AuctionId);

                var auction = await _bidOperation.GetAuctionByIdAsync(paymentAttempt.AuctionId);
                if (auction != null)
                {
                    auction.Status = AuctionStatus.Failed;
                    await _bidOperation.UpdateAuctionAsync(auction);
                }

                return;
            }

            // Get all bids for this auction ordered by amount
            var allBids = await _paymentOperation.GetBidsByAuctionOrderedAsync(paymentAttempt.AuctionId);
            
            // Get all previous bidders who already had a chance
            var previousBidders = new HashSet<int>();
            var allPreviousAttempts = await GetAllPaymentAttemptsForAuction(paymentAttempt.AuctionId);
            foreach (var attempt in allPreviousAttempts)
            {
                previousBidders.Add(attempt.BidderId);
            }

            // Find next eligible bidder (highest bid among those who haven't tried yet)
            var nextBid = allBids.FirstOrDefault(b => !previousBidders.Contains(b.BidderId));
            
            if (nextBid == null)
            {
                // No more bidders to try
                _logger.LogWarning(
                    "No more eligible bidders for auction {AuctionId}, marking as failed",
                    paymentAttempt.AuctionId);

                var auction = await _bidOperation.GetAuctionByIdAsync(paymentAttempt.AuctionId);
                if (auction != null)
                {
                    auction.Status = AuctionStatus.Failed;
                    await _bidOperation.UpdateAuctionAsync(auction);
                }

                return;
            }

            // Create payment attempt for next bidder
            _logger.LogInformation(
                "Creating retry payment attempt for auction {AuctionId}, next bidder {BidderId}, attempt {AttemptNumber}",
                paymentAttempt.AuctionId, nextBid.BidderId, attemptCount + 1);

            var now = DateTime.UtcNow;
            var newPaymentAttempt = new PaymentAttempt
            {
                AuctionId = paymentAttempt.AuctionId,
                BidderId = nextBid.BidderId,
                Status = PaymentStatus.Pending,
                AttemptNumber = attemptCount + 1,
                AttemptTime = now,
                ExpiryTime = now.AddMinutes(_paymentSettings.WindowMinutes),
                Amount = nextBid.Amount
            };

            var createdAttempt = await _paymentOperation.CreatePaymentAttemptAsync(newPaymentAttempt);

            // Send email notification to next bidder
            await _emailService.SendPaymentNotificationAsync(
                createdAttempt.Bidder,
                createdAttempt.Auction,
                createdAttempt);

            _logger.LogInformation(
                "Created retry payment attempt {PaymentId} for auction {AuctionId}",
                createdAttempt.PaymentId, paymentAttempt.AuctionId);
        }

        /// <summary>
        /// Handles payment failure by creating a failed transaction and triggering retry
        /// </summary>
        private async Task<Transaction> HandlePaymentFailure(
            PaymentAttempt paymentAttempt,
            decimal confirmedAmount,
            string reason)
        {
            _logger.LogWarning(
                "Payment failed for payment attempt {PaymentId}. Reason: {Reason}",
                paymentAttempt.PaymentId, reason);

            // Update payment attempt
            paymentAttempt.Status = PaymentStatus.Failed;
            paymentAttempt.ConfirmedAmount = confirmedAmount;
            await _paymentOperation.UpdatePaymentAttemptAsync(paymentAttempt);

            // Create failed transaction
            var transaction = new Transaction
            {
                PaymentId = paymentAttempt.PaymentId,
                Status = TransactionStatus.Failed,
                Amount = confirmedAmount,
                Timestamp = DateTime.UtcNow
            };

            var createdTransaction = await _paymentOperation.CreateTransactionAsync(transaction);

            // Trigger retry process immediately (don't wait for background service)
            await ProcessFailedPaymentAsync(paymentAttempt.PaymentId);

            return createdTransaction;
        }

        /// <summary>
        /// Gets all payment attempts for an auction (helper method)
        /// </summary>
        private async Task<List<PaymentAttempt>> GetAllPaymentAttemptsForAuction(int auctionId)
        {
            return await _paymentOperation.GetAllPaymentAttemptsForAuctionAsync(auctionId);
        }
    }
}

