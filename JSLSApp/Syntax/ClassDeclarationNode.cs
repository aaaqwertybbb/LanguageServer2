using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class ClassDeclarationNode : SyntaxNode
{
    public ClassDeclarationNode(int startLine, int startCharacter, int endLine, int endCharacter)
    {
        Start = new Position(startLine, startCharacter);
        End = new Position(endLine, endCharacter);

        _scope.Type = BodyKind.ClassBody;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.ClassDeclarationNode;

    public override Position Start { get; set; }
    public override Position End { get; set; }
    public override IdKind Id_type => IdKind.Identifier;

    public override bool IsScopeProvider => true;
    public override bool IsIdentifiable => true;

    private Scope _scope;
    private int _identifierHash;

    public override ref Scope GetScope() => ref _scope;
    public override ref int GetIdentifierHash() => ref _identifierHash;
}

/// <summary>
/// Might be wrong right off the bat to use a global node rather than just inferring global due to a lack of parent idk I'll figure it out at some later point I just gotta type something first.
///
/// Actually it says body in the first json example.
/// < {
/// <   "type": "Program",
/// <   "body": [...
/// </summary>
//public class GlobalNode : SyntaxNode
//{
//    public override SyntaxKind SyntaxKind => SyntaxKind.GlobalNode;
//}


