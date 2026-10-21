using JSLSApp.LspTypes;

namespace JSLSApp;

public struct SyntaxToken(SyntaxKind syntaxKind, Position position, int length)
{
    public SyntaxToken(SyntaxKind syntaxKind, Position position, int length, int start)
        : this(syntaxKind, position, length)
    {
        Start = start;
    }

    public SyntaxKind SyntaxKind { get; set; } = syntaxKind;
    public Position Position { get; set; } = position;
    public int Start { get; set; }
    public int Length { get; set; } = length;
}

public enum TrackedSyntaxKind : byte
{
    None = 0,
    String = 1,
    Comment = 2,
    Keyword = 3,
    KeywordControl = 4,
    Identifier = 5,
}

public static class SyntaxExtensions
{
    /// <summary>
    /// TODO: optimize setting these bytes and etc...
    /// </summary>
    public static void SetSyntaxHighlighting(this byte[] list, int start, int length, TrackedSyntaxKind kind)
    {
        for (var i = 0; i < length; i++)
        {
            list[start + i] = (byte)kind;
        }
    }
}
