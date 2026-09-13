using JSLSApp.LspTypes;

namespace JSLSApp;

public struct SyntaxToken(SyntaxKind syntaxKind, Position position, int length)
{
    public SyntaxKind SyntaxKind { get; set; } = syntaxKind;
    public Position Position { get; set; } = position;
    public int Length { get; set; } = length;
}

public enum TrackedSyntaxKind
{
    None = 0,
    String = 1,
    Comment = 2,
}

public struct TrackedSyntax(TrackedSyntaxKind trackedSyntaxKind, int start, int length)
{
    public TrackedSyntaxKind TrackedSyntaxKind { get; set; } = trackedSyntaxKind;
    public int Start { get; set; } = start;
    public int Length { get; set; } = length;
}
