using System.Linq.Expressions;

namespace WebApiTemplate.Service.QueryParser
{
    /// <summary>
    /// Interface for ASQL (Auction Search Query Language) parser
    /// </summary>
    public interface IAsqlParser
    {
        /// <summary>
        /// Parses an ASQL query string and applies it to a queryable
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="queryable">Base queryable to apply filter to</param>
        /// <param name="asqlQuery">ASQL query string</param>
        /// <returns>Filtered queryable</returns>
        IQueryable<T> ApplyQuery<T>(IQueryable<T> queryable, string asqlQuery) where T : class;

        /// <summary>
        /// Validates an ASQL query syntax
        /// </summary>
        /// <param name="asqlQuery">ASQL query string</param>
        /// <returns>Validation result with error message if invalid</returns>
        (bool IsValid, string? ErrorMessage) ValidateQuery(string asqlQuery);
    }
}

