using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace WebApiTemplate.Service.QueryParser
{
    /// <summary>
    /// Parser for ASQL (Auction Search Query Language)
    /// Supports: =, !=, <, <=, >, >=, in operators
    /// Supports: AND, OR logical operators (no nesting)
    /// </summary>
    public class AsqlParser : IAsqlParser
    {
        private readonly ILogger<AsqlParser> _logger;

        public AsqlParser(ILogger<AsqlParser> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates an ASQL query syntax
        /// </summary>
        public (bool IsValid, string? ErrorMessage) ValidateQuery(string asqlQuery)
        {
            if (string.IsNullOrWhiteSpace(asqlQuery))
            {
                return (false, "Query cannot be empty");
            }

            try
            {
                var tokens = Tokenize(asqlQuery);
                Parse(tokens);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Applies an ASQL query to a queryable
        /// </summary>
        public IQueryable<T> ApplyQuery<T>(IQueryable<T> queryable, string asqlQuery) where T : class
        {
            if (string.IsNullOrWhiteSpace(asqlQuery))
            {
                return queryable;
            }

            try
            {
                var tokens = Tokenize(asqlQuery);
                var expression = Parse(tokens);
                var predicate = BuildPredicate<T>(expression);

                _logger.LogInformation("Applied ASQL query: {Query}", asqlQuery);

                return queryable.Where(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing ASQL query: {Query}", asqlQuery);
                throw new ArgumentException($"Invalid ASQL query: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tokenizes an ASQL query string
        /// </summary>
        private List<AsqlToken> Tokenize(string query)
        {
            var tokens = new List<AsqlToken>();
            var position = 0;

            while (position < query.Length)
            {
                // Skip whitespace
                if (char.IsWhiteSpace(query[position]))
                {
                    position++;
                    continue;
                }

                // Check for operators (must check >= and <= before > and <)
                if (position < query.Length - 1)
                {
                    var twoChar = query.Substring(position, 2);
                    if (twoChar == ">=" || twoChar == "<=" || twoChar == "!=" || twoChar == "in")
                    {
                        if (twoChar == "in" && position + 2 < query.Length && !char.IsWhiteSpace(query[position + 2]))
                        {
                            // "in" must be followed by whitespace or end of string
                        }
                        else
                        {
                            tokens.Add(new AsqlToken(AsqlTokenType.Operator, twoChar, position));
                            position += 2;
                            continue;
                        }
                    }
                }

                // Check for single-char operators
                if (query[position] == '=' || query[position] == '<' || query[position] == '>')
                {
                    tokens.Add(new AsqlToken(AsqlTokenType.Operator, query[position].ToString(), position));
                    position++;
                    continue;
                }

                // Check for brackets and comma
                if (query[position] == '[')
                {
                    tokens.Add(new AsqlToken(AsqlTokenType.OpenBracket, "[", position));
                    position++;
                    continue;
                }

                if (query[position] == ']')
                {
                    tokens.Add(new AsqlToken(AsqlTokenType.CloseBracket, "]", position));
                    position++;
                    continue;
                }

                if (query[position] == ',')
                {
                    tokens.Add(new AsqlToken(AsqlTokenType.Comma, ",", position));
                    position++;
                    continue;
                }

                // Check for quoted strings
                if (query[position] == '"')
                {
                    var startPos = position;
                    position++; // Skip opening quote
                    var value = "";

                    while (position < query.Length && query[position] != '"')
                    {
                        value += query[position];
                        position++;
                    }

                    if (position >= query.Length)
                    {
                        throw new ArgumentException($"Unterminated string at position {startPos}");
                    }

                    position++; // Skip closing quote
                    tokens.Add(new AsqlToken(AsqlTokenType.Value, value, startPos));
                    continue;
                }

                // Check for keywords (AND, OR, in)
                var remainingQuery = query.Substring(position);
                var keywordMatch = Regex.Match(remainingQuery, @"^(AND|OR|in)\b", RegexOptions.IgnoreCase);
                if (keywordMatch.Success)
                {
                    var keyword = keywordMatch.Value.ToUpper();
                    if (keyword == "AND" || keyword == "OR")
                    {
                        tokens.Add(new AsqlToken(AsqlTokenType.LogicalOperator, keyword, position));
                    }
                    else if (keyword == "IN")
                    {
                        tokens.Add(new AsqlToken(AsqlTokenType.Operator, keyword, position));
                    }
                    position += keyword.Length;
                    continue;
                }

                // Check for field names or numeric values
                var match = Regex.Match(remainingQuery, @"^[a-zA-Z_][a-zA-Z0-9_]*");
                if (match.Success)
                {
                    var identifier = match.Value;
                    // Check if we're expecting a field or value based on previous token
                    var isField = tokens.Count == 0 || 
                                  tokens.Last().Type == AsqlTokenType.LogicalOperator ||
                                  tokens.Last().Type == AsqlTokenType.OpenBracket ||
                                  tokens.Last().Type == AsqlTokenType.Comma;

                    tokens.Add(new AsqlToken(isField ? AsqlTokenType.Field : AsqlTokenType.Value, identifier, position));
                    position += identifier.Length;
                    continue;
                }

                // Check for numeric values
                var numMatch = Regex.Match(remainingQuery, @"^-?\d+(\.\d+)?");
                if (numMatch.Success)
                {
                    tokens.Add(new AsqlToken(AsqlTokenType.Value, numMatch.Value, position));
                    position += numMatch.Length;
                    continue;
                }

                throw new ArgumentException($"Unexpected character '{query[position]}' at position {position}");
            }

            tokens.Add(new AsqlToken(AsqlTokenType.EndOfQuery, "", position));
            return tokens;
        }

        /// <summary>
        /// Parses tokens into an expression tree
        /// </summary>
        private AsqlExpression Parse(List<AsqlToken> tokens)
        {
            var index = 0;
            return ParseLogicalExpression(tokens, ref index);
        }

        /// <summary>
        /// Parses logical expressions (AND/OR)
        /// </summary>
        private AsqlExpression ParseLogicalExpression(List<AsqlToken> tokens, ref int index)
        {
            var left = ParseComparisonExpression(tokens, ref index);

            while (index < tokens.Count && tokens[index].Type == AsqlTokenType.LogicalOperator)
            {
                var op = tokens[index].Value;
                index++;

                var right = ParseComparisonExpression(tokens, ref index);
                left = new LogicalExpression(left, op, right);
            }

            return left;
        }

        /// <summary>
        /// Parses comparison expressions
        /// </summary>
        private AsqlExpression ParseComparisonExpression(List<AsqlToken> tokens, ref int index)
        {
            if (index >= tokens.Count || tokens[index].Type != AsqlTokenType.Field)
            {
                throw new ArgumentException($"Expected field name at position {index}");
            }

            var field = tokens[index].Value;
            index++;

            if (index >= tokens.Count || tokens[index].Type != AsqlTokenType.Operator)
            {
                throw new ArgumentException($"Expected operator after field '{field}'");
            }

            var op = tokens[index].Value.ToUpper();
            index++;

            // Handle IN operator
            if (op == "IN")
            {
                return ParseInExpression(tokens, ref index, field);
            }

            // Handle regular comparison operators
            if (index >= tokens.Count || tokens[index].Type != AsqlTokenType.Value)
            {
                throw new ArgumentException($"Expected value after operator '{op}'");
            }

            var value = ParseValue(tokens[index].Value);
            index++;

            return new ComparisonExpression(field, op, value);
        }

        /// <summary>
        /// Parses IN expression
        /// </summary>
        private InExpression ParseInExpression(List<AsqlToken> tokens, ref int index, string field)
        {
            if (index >= tokens.Count || tokens[index].Type != AsqlTokenType.OpenBracket)
            {
                throw new ArgumentException($"Expected '[' after 'in' operator for field '{field}'");
            }

            index++; // Skip [

            var values = new List<object>();

            while (index < tokens.Count && tokens[index].Type != AsqlTokenType.CloseBracket)
            {
                if (tokens[index].Type == AsqlTokenType.Value)
                {
                    values.Add(ParseValue(tokens[index].Value));
                    index++;

                    if (index < tokens.Count && tokens[index].Type == AsqlTokenType.Comma)
                    {
                        index++; // Skip comma
                    }
                }
                else
                {
                    throw new ArgumentException($"Expected value in array at position {index}");
                }
            }

            if (index >= tokens.Count || tokens[index].Type != AsqlTokenType.CloseBracket)
            {
                throw new ArgumentException($"Expected ']' to close array for field '{field}'");
            }

            index++; // Skip ]

            return new InExpression(field, values);
        }

        /// <summary>
        /// Parses a value token into its appropriate type
        /// </summary>
        private object ParseValue(string value)
        {
            // Try to parse as number
            if (decimal.TryParse(value, out var decimalValue))
            {
                return decimalValue;
            }

            if (int.TryParse(value, out var intValue))
            {
                return intValue;
            }

            // Otherwise treat as string
            return value;
        }

        /// <summary>
        /// Builds a LINQ predicate from an expression tree
        /// </summary>
        private Expression<Func<T, bool>> BuildPredicate<T>(AsqlExpression expression)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var body = BuildExpressionBody(expression, parameter);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        /// <summary>
        /// Builds the expression body recursively
        /// </summary>
        private Expression BuildExpressionBody(AsqlExpression expression, ParameterExpression parameter)
        {
            if (expression is ComparisonExpression comparison)
            {
                return BuildComparisonExpression(comparison, parameter);
            }
            else if (expression is InExpression inExpr)
            {
                return BuildInExpression(inExpr, parameter);
            }
            else if (expression is LogicalExpression logical)
            {
                var left = BuildExpressionBody(logical.Left, parameter);
                var right = BuildExpressionBody(logical.Right, parameter);

                if (logical.Operator == "AND")
                {
                    return Expression.AndAlso(left, right);
                }
                else if (logical.Operator == "OR")
                {
                    return Expression.OrElse(left, right);
                }
            }

            throw new ArgumentException($"Unknown expression type: {expression.GetType().Name}");
        }

        /// <summary>
        /// Builds a comparison expression
        /// </summary>
        private Expression BuildComparisonExpression(ComparisonExpression comparison, ParameterExpression parameter)
        {
            var property = Expression.Property(parameter, ToPascalCase(comparison.Field));
            var propertyType = property.Type;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // Convert value to property type
            object convertedValue;
            try
            {
                if (comparison.Value == null)
                {
                    convertedValue = null!;
                }
                else if (underlyingType == typeof(decimal))
                {
                    convertedValue = Convert.ToDecimal(comparison.Value);
                }
                else if (underlyingType == typeof(int))
                {
                    convertedValue = Convert.ToInt32(comparison.Value);
                }
                else if (underlyingType == typeof(DateTime))
                {
                    convertedValue = Convert.ToDateTime(comparison.Value);
                }
                else
                {
                    convertedValue = comparison.Value.ToString()!;
                }
            }
            catch
            {
                throw new ArgumentException($"Cannot convert value '{comparison.Value}' to type {underlyingType.Name} for field '{comparison.Field}'");
            }

            var constant = Expression.Constant(convertedValue, propertyType);

            return comparison.Operator switch
            {
                "=" => Expression.Equal(property, constant),
                "!=" => Expression.NotEqual(property, constant),
                "<" => Expression.LessThan(property, constant),
                "<=" => Expression.LessThanOrEqual(property, constant),
                ">" => Expression.GreaterThan(property, constant),
                ">=" => Expression.GreaterThanOrEqual(property, constant),
                _ => throw new ArgumentException($"Unknown operator: {comparison.Operator}")
            };
        }

        /// <summary>
        /// Builds an IN expression
        /// </summary>
        private Expression BuildInExpression(InExpression inExpr, ParameterExpression parameter)
        {
            var property = Expression.Property(parameter, ToPascalCase(inExpr.Field));
            var propertyType = property.Type;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // Create a list constant
            var listType = typeof(List<>).MakeGenericType(underlyingType);
            var list = Activator.CreateInstance(listType);
            var addMethod = listType.GetMethod("Add");

            // Convert all values to the property type and add to list
            foreach (var value in inExpr.Values)
            {
                object convertedValue;
                if (underlyingType == typeof(decimal))
                    convertedValue = Convert.ToDecimal(value);
                else if (underlyingType == typeof(int))
                    convertedValue = Convert.ToInt32(value);
                else
                    convertedValue = value.ToString()!;

                addMethod!.Invoke(list, new object[] { convertedValue });
            }

            var listConstant = Expression.Constant(list);
            var containsMethod = listType.GetMethod("Contains", new[] { underlyingType });

            return Expression.Call(listConstant, containsMethod!, property);
        }

        /// <summary>
        /// Converts a field name to PascalCase to match entity properties
        /// </summary>
        private string ToPascalCase(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return fieldName;

            // Convert first character to uppercase
            return char.ToUpper(fieldName[0]) + fieldName.Substring(1);
        }
    }
}

