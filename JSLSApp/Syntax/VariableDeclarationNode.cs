using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class VariableDeclarationNode : SyntaxNode
{
    public VariableDeclarationNode(int distanceFromPreviousNode, int length, SyntaxNode? parentScopeProvider)
    {
        DistanceFromPreviousNode = distanceFromPreviousNode;
        Length = length;
        ParentScopeProvider = parentScopeProvider;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.VariableDeclarationNode;

    public override int DistanceFromPreviousNode { get; set; }
    public override int Length { get; set; }

    public SyntaxNode? ParentScopeProvider { get; set; }

    public override bool IsScopeProvider => false;
    public override bool IsIdentifiable => true;

    private int _identifierHash;

    public override ref int GetIdentifierHash() => ref _identifierHash;
}
