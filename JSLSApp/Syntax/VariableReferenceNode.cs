using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

public class VariableReferenceNode : SyntaxNode
{
    public VariableReferenceNode(int startLine, int startCharacter, int endLine, int endCharacter, int definitionLine)
    {
        Start = new Position(startLine, startCharacter);
        End = new Position(endLine, endCharacter);
        DefinitionLine = definitionLine;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.VariableReferenceNode;

    public override Position Start { get; set; }
    public override Position End { get; set; }
    public override IdKind Id_type => IdKind.Identifier;
    public int DefinitionLine { get; set; }

    public override bool IsScopeProvider => false;
    public override bool IsIdentifiable => true;

    private int _identifierHash;

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


