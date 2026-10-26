namespace JSLSApp.Syntax;

public enum BodyKind
{
    GlobalBody,
    ClassBody,
    FunctionBody,
    IfStatementBody,

    EXTERNAL_FunctionBody,
    ForStatementBody,
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


