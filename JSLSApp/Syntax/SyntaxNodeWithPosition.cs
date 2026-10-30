using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public struct SyntaxNodeWithPosition(SyntaxNode node, Position position, int positionIndex)
{
    public SyntaxNode Node { get; set; } = node;
    public Position Position { get; set; } = position;
    public int PositionIndex { get; set; } = positionIndex;
}
