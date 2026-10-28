using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class BlockNode : SyntaxNode
{
    public BlockNode(int distanceFromPreviousNode, int length)
    {
        DistanceFromPreviousNode = distanceFromPreviousNode;
        Length = length;

        _scope.Type = BodyKind.BlockBody;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.BlockNode;

    public override int DistanceFromPreviousNode { get; set; }
    public override int Length { get; set; }
    public override IdKind Id_type => IdKind.Identifier;
    
    public override bool IsScopeProvider => true;
    public override bool IsIdentifiable => false;

    private Scope _scope;

    public override ref Scope GetScope() => ref _scope;
}
