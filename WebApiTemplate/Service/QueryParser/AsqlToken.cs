namespace WebApiTemplate.Service.QueryParser
{
    /// <summary>
    /// Represents the type of token in an ASQL query
    /// </summary>
    public enum AsqlTokenType
    {
        Field,
        Operator,
        Value,
        LogicalOperator,
        OpenBracket,
        CloseBracket,
        Comma,
        EndOfQuery
    }

    /// <summary>
    /// Represents a token in an ASQL query
    /// </summary>
    public class AsqlToken
    {
        public AsqlTokenType Type { get; set; }
        public string Value { get; set; } = string.Empty;
        public int Position { get; set; }

        public AsqlToken(AsqlTokenType type, string value, int position)
        {
            Type = type;
            Value = value;
            Position = position;
        }

        public override string ToString()
        {
            return $"{Type}: {Value} (pos: {Position})";
        }
    }
}

