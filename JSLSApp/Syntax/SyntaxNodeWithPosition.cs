using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public struct SyntaxNodeWithPosition(SyntaxNode node, Position position)
{
    public SyntaxNode Node { get; set; } = node;
    public Position Position { get; set; } = position;
}
