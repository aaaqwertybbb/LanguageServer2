using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class FunctionDeclarationNode : SyntaxNode
{
    public FunctionDeclarationNode(string id_name, int startLine, int startCharacter, int endLine, int endCharacter)
    {
        Id_name = id_name;
        Start = new Position(startLine, startCharacter);
        End = new Position(endLine, endCharacter);

        _scope.Type = BodyKind.FunctionBody;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.FunctionDeclarationNode;

    public override Position Start { get; set; }
    public override Position End { get; set; }
    public override IdKind Id_type => IdKind.Identifier;
    public override string Id_name { get; }

    public override bool IsScopeProvider => true;

    private Scope _scope;

    public override ref Scope GetScope() => ref _scope;
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


