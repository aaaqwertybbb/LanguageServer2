using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class ForStatementNode : SyntaxNode
{
    public ForStatementNode(int distanceFromPreviousNode, int length)
    {
        DistanceFromPreviousNode = distanceFromPreviousNode;
        Length = length;

        _scope.Type = BodyKind.ForStatementBody;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.ForStatementNode;

    public override int DistanceFromPreviousNode { get; set; }
    public override int Length { get; set; }
    
    public override bool IsScopeProvider => true;
    public override bool IsIdentifiable => false;

    private Scope _scope;

    public override ref Scope GetScope() => ref _scope;
}
