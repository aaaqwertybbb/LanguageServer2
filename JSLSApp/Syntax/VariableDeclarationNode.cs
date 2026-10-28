using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class VariableDeclarationNode : SyntaxNode
{
    public VariableDeclarationNode(int distanceFromPreviousNode, int length)
    {
        DistanceFromPreviousNode = distanceFromPreviousNode;
        Length = length;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.VariableDeclarationNode;

    public override int DistanceFromPreviousNode { get; set; }
    public override int Length { get; set; }
    public override IdKind Id_type => IdKind.Identifier;

    public override bool IsScopeProvider => false;
    public override bool IsIdentifiable => true;

    private int _identifierHash;

    public override ref int GetIdentifierHash() => ref _identifierHash;
}
