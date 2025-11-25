namespace WebApiTemplate.Service.QueryParser
{
    /// <summary>
    /// Represents an ASQL expression node
    /// </summary>
    public abstract class AsqlExpression
    {
    }

    /// <summary>
    /// Represents a comparison expression (e.g., field = value)
    /// </summary>
    public class ComparisonExpression : AsqlExpression
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public object? Value { get; set; }

        public ComparisonExpression(string field, string operatorSymbol, object? value)
        {
            Field = field;
            Operator = operatorSymbol;
            Value = value;
        }
    }

    /// <summary>
    /// Represents an IN expression (e.g., field in [value1, value2])
    /// </summary>
    public class InExpression : AsqlExpression
    {
        public string Field { get; set; } = string.Empty;
        public List<object> Values { get; set; } = new List<object>();

        public InExpression(string field, List<object> values)
        {
            Field = field;
            Values = values;
        }
    }

    /// <summary>
    /// Represents a logical expression (AND/OR)
    /// </summary>
    public class LogicalExpression : AsqlExpression
    {
        public AsqlExpression Left { get; set; }
        public string Operator { get; set; } = string.Empty; // AND or OR
        public AsqlExpression Right { get; set; }

        public LogicalExpression(AsqlExpression left, string operatorSymbol, AsqlExpression right)
        {
            Left = left;
            Operator = operatorSymbol;
            Right = right;
        }
    }
}

