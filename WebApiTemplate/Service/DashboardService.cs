using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApiTemplate.Constants;
using WebApiTemplate.Extensions;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database;

namespace WebApiTemplate.Service
{
    /// <summary>
    /// Service for dashboard metrics and analytics
    /// </summary>
    public class DashboardService : Interface.IDashboardService
    {
        private readonly WenApiTemplateDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            WenApiTemplateDbContext context,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves comprehensive dashboard metrics with optional date filtering
        /// </summary>
        /// <param name="fromDate">Optional start date for filtering</param>
        /// <param name="toDate">Optional end date for filtering</param>
        /// <returns>Dashboard metrics including auction counts and top bidders</returns>
        public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(DateTime? fromDate, DateTime? toDate)
        {
            _logger.LogInformation("Retrieving dashboard metrics. FromDate: {FromDate}, ToDate: {ToDate}", 
                fromDate, toDate);

            // Base query for auctions with optional date filtering
            var auctionsQuery = _context.Auctions.AsNoTracking().AsQueryable();

            if (fromDate.HasValue)
            {
                auctionsQuery = auctionsQuery.Where(a => a.ExpiryTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                auctionsQuery = auctionsQuery.Where(a => a.ExpiryTime <= toDate.Value);
            }

            // Get auction counts using extension methods
            var activeCount = await auctionsQuery
                .WhereStatus(AuctionStatus.Active)
                .CountAsync();

            var pendingPaymentCount = await auctionsQuery
                .WhereStatus(AuctionStatus.PendingPayment)
                .CountAsync();

            var completedCount = await auctionsQuery
                .WhereStatus(AuctionStatus.Completed)
                .CountAsync();

            // Failed count: explicitly failed OR pending payment with expired window
            var failedCount = await auctionsQuery
                .WhereStatus(AuctionStatus.Failed)
                .CountAsync();

            // Also count pending payments with expired payment windows using extensions
            var expiredPendingPayments = await _context.PaymentAttempts
                .AsNoTracking()
                .WherePending()
                .WhereExpired()
                .Select(pa => pa.AuctionId)
                .Distinct()
                .CountAsync();

            // Add expired pending payments to failed count
            var totalFailedCount = failedCount + expiredPendingPayments;

            // Get top bidders
            var topBidders = await GetTopBiddersAsync(fromDate, toDate);

            var metrics = new DashboardMetricsDto
            {
                ActiveCount = activeCount,
                PendingPayment = pendingPaymentCount,
                CompletedCount = completedCount,
                FailedCount = totalFailedCount,
                TopBidders = topBidders
            };

            _logger.LogInformation(
                "Dashboard metrics retrieved. Active: {Active}, Pending: {Pending}, Completed: {Completed}, Failed: {Failed}, TopBidders: {TopBidders}",
                activeCount, pendingPaymentCount, completedCount, totalFailedCount, topBidders.Count);

            return metrics;
        }

        /// <summary>
        /// Gets top 5 bidders ranked by total bid amount with statistics
        /// </summary>
        private async Task<List<TopBidderDto>> GetTopBiddersAsync(DateTime? fromDate, DateTime? toDate)
        {
            // Base query for bids with optional date filtering
            var bidsQuery = _context.Bids.AsNoTracking().AsQueryable();

            if (fromDate.HasValue)
            {
                bidsQuery = bidsQuery.Where(b => b.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                bidsQuery = bidsQuery.Where(b => b.Timestamp <= toDate.Value);
            }

            // Group by bidder and calculate statistics
            var bidderStats = await bidsQuery
                .GroupBy(b => new { b.BidderId, b.Bidder.Email })
                .Select(g => new
                {
                    UserId = g.Key.BidderId,
                    Username = g.Key.Email,
                    TotalBidAmount = g.Sum(b => b.Amount),
                    TotalBidsCount = g.Count(),
                    UniqueAuctions = g.Select(b => b.AuctionId).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalBidAmount)
                .Take(5)
                .ToListAsync();

            // For each top bidder, calculate auctions won
            var topBidderDtos = new List<TopBidderDto>();

            foreach (var bidder in bidderStats)
            {
                // Count auctions won by this bidder (completed auctions where their bid is the highest)
                var auctionsWonQuery = _context.Auctions
                    .AsNoTracking()
                    .Where(a => a.Status == AuctionStatus.Completed && a.HighestBid!.BidderId == bidder.UserId);

                // Apply date filtering to auctions won
                if (fromDate.HasValue)
                {
                    auctionsWonQuery = auctionsWonQuery.Where(a => a.ExpiryTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    auctionsWonQuery = auctionsWonQuery.Where(a => a.ExpiryTime <= toDate.Value);
                }

                var auctionsWon = await auctionsWonQuery.CountAsync();

                // Calculate win rate (auctions won / unique auctions participated)
                var winRate = bidder.UniqueAuctions > 0
                    ? Math.Round((decimal)auctionsWon / bidder.UniqueAuctions * 100, 2)
                    : 0;

                topBidderDtos.Add(new TopBidderDto
                {
                    UserId = bidder.UserId,
                    Username = bidder.Username,
                    TotalBidAmount = bidder.TotalBidAmount,
                    TotalBidsCount = bidder.TotalBidsCount,
                    AuctionsWon = auctionsWon,
                    WinRate = winRate
                });
            }

            return topBidderDtos;
        }
    }
}

