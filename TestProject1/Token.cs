namespace TestProject1;

/// <summary>
/// TODO: Remind yourself about readonly struct defensive copies.
/// </summary>
/// <param name="type"></param>
/// <param name="start"><inheritdoc cref="Start" path="/summary"/></param>
/// <param name="end"><inheritdoc cref="End" path="/summary"/></param>
internal readonly struct Token(TokenType type, int start, int end)
{
    internal TokenType Type { get; } = type;
    /// <summary>inclusive character position within text</summary>
    internal int Start { get; } = start;
    /// <summary>exclusive character position within text</summary>
    internal int End { get; } = end;

    internal string ToTestString(string source)
    {
        return Type switch
        {
            TokenType.EOF => "EOF",
            TokenType.INT => $"INT({source[Start..End]})",
            _ => throw new NotImplementedException(),
        };
    }
}
