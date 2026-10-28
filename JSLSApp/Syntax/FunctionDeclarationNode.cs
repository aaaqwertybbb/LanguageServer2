using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

/// <summary>
/// You could do the relative positions by: function definition arguments are a distance from the function identifier.
/// And then the 'FunctionDeclarationNode' is a scope provider. So any children of that scope are a distance from the start of the scope.
/// </summary>
public class FunctionDeclarationNode : SyntaxNode
{
    public FunctionDeclarationNode(int distanceFromPreviousNode, int length)
    {
        DistanceFromPreviousNode = distanceFromPreviousNode;
        Length = length;

        _scope.Type = BodyKind.FunctionBody;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.FunctionDeclarationNode;

    public override int DistanceFromPreviousNode { get; set; }
    public override int Length { get; set; }
    
    

    public override bool IsScopeProvider => true;
    public override bool IsIdentifiable => true;

    private Scope _scope;
    private int _identifierHash;

    public override ref Scope GetScope() => ref _scope;
    public override ref int GetIdentifierHash() => ref _identifierHash;
}
