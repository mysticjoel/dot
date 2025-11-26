using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApiTemplate.Constants;
using WebApiTemplate.Models;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Controllers
{
    /// <summary>
    /// Controller for dashboard metrics and analytics
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;
        private readonly IValidator<DashboardFilterDto> _filterValidator;

        public DashboardController(
            IDashboardService dashboardService,
            ILogger<DashboardController> logger,
            IValidator<DashboardFilterDto> filterValidator)
        {
            _dashboardService = dashboardService;
            _logger = logger;
            _filterValidator = filterValidator;
        }

        /// <summary>
        /// Validates a DTO using FluentValidation and returns BadRequest if invalid
        /// </summary>
        private IActionResult? ValidateDto<T>(FluentValidation.Results.ValidationResult validationResult)
        {
            if (validationResult.IsValid)
                return null;

            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return BadRequest(new { message = "Validation failed", errors });
        }

        /// <summary>
        /// Get comprehensive dashboard metrics for the auction system
        /// </summary>
        /// <param name="fromDate">Optional start date for filtering (format: yyyy-MM-dd)</param>
        /// <param name="toDate">Optional end date for filtering (format: yyyy-MM-dd)</param>
        /// <returns>Dashboard metrics including auction counts and top bidders</returns>
        /// <response code="200">Metrics retrieved successfully</response>
        /// <response code="400">Validation error (invalid date range)</response>
        /// <response code="401">Unauthorized - not authenticated</response>
        /// <response code="403">Forbidden - not an admin</response>
        [HttpGet]
        [ProducesResponseType(typeof(DashboardMetricsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDashboardMetrics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                // Create filter DTO and validate
                var filterDto = new DashboardFilterDto
                {
                    FromDate = fromDate,
                    ToDate = toDate
                };

                var validationResult = await _filterValidator.ValidateAsync(filterDto);
                var validationError = ValidateDto<DashboardFilterDto>(validationResult);
                if (validationError != null)
                    return validationError;

                _logger.LogInformation("Dashboard metrics requested. FromDate: {FromDate}, ToDate: {ToDate}", 
                    fromDate, toDate);

                var metrics = await _dashboardService.GetDashboardMetricsAsync(fromDate, toDate);

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard metrics");
                return StatusCode(500, new { message = "An error occurred while retrieving dashboard metrics." });
            }
        }
    }
}

