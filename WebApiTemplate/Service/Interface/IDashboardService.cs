using System;
using System.Threading.Tasks;
using WebApiTemplate.Models;

namespace WebApiTemplate.Service.Interface
{
    /// <summary>
    /// Service interface for dashboard metrics and analytics
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Retrieves comprehensive dashboard metrics with optional date filtering
        /// </summary>
        /// <param name="fromDate">Optional start date for filtering</param>
        /// <param name="toDate">Optional end date for filtering</param>
        /// <returns>Dashboard metrics including auction counts and top bidders</returns>
        Task<DashboardMetricsDto> GetDashboardMetricsAsync(DateTime? fromDate, DateTime? toDate);
    }
}

