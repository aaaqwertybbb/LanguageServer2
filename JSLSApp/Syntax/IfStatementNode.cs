using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class IfStatementNode : SyntaxNode
{
    public IfStatementNode(int startLine, int startCharacter, int endLine, int endCharacter)
    {
        Start = new Position(startLine, startCharacter);
        End = new Position(endLine, endCharacter);

        _scope.Type = BodyKind.IfStatementBody;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.IfStatementNode;

    public override Position Start { get; set; }
    public override Position End { get; set; }
    public override IdKind Id_type => IdKind.Identifier;
    
    public override bool IsScopeProvider => true;
    public override bool IsIdentifiable => false;

    private Scope _scope;

    public override ref Scope GetScope() => ref _scope;
}
