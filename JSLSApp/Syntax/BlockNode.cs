using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class BlockNode : SyntaxNode
{
    public BlockNode(int startLine, int startCharacter, int endLine, int endCharacter)
    {
        Start = new Position(startLine, startCharacter);
        End = new Position(endLine, endCharacter);

        _scope.Type = BodyKind.BlockBody;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.BlockNode;

    public override Position Start { get; set; }
    public override Position End { get; set; }
    public override IdKind Id_type => IdKind.Identifier;
    
    public override bool IsScopeProvider => true;
    public override bool IsIdentifiable => false;

    private Scope _scope;

    public override ref Scope GetScope() => ref _scope;
}
