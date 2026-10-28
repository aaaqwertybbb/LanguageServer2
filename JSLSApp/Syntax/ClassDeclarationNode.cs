using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class ClassDeclarationNode : SyntaxNode
{
    public ClassDeclarationNode(int distanceFromPreviousNode, int length)
    {
        DistanceFromPreviousNode = distanceFromPreviousNode;
        Length = length;

        _scope.Type = BodyKind.ClassBody;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.ClassDeclarationNode;

    public override int DistanceFromPreviousNode { get; set; }
    public override int Length { get; set; }

    public override bool IsScopeProvider => true;
    public override bool IsIdentifiable => true;

    private Scope _scope;
    private int _identifierHash;

    public override ref Scope GetScope() => ref _scope;
    public override ref int GetIdentifierHash() => ref _identifierHash;
}
