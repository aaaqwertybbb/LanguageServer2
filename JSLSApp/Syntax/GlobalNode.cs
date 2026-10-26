using JSLSApp.LspTypes;

namespace JSLSApp.Syntax;

/// <summary>
/// Some result caching is based on the node that the cursor is "closest to".
/// Sometimes that closest node is "null" because a GlobalNode at the moment isn't actually part of the "CompilationUnit".
/// So you can use this as a standin to represent the fact that your result was relative to the global scope.
/// If nothing about the node is needed due to checks for 'SyntaxKind.GlobalNode' you can just use the 'GlobalNode.GLOBAL_NODE' instance.
/// But I'm deciding whether explicitly checking the syntax kind for GlobalNode is something I want to do or not.
/// </summary>
public class GlobalNode : SyntaxNode
{
    //public static readonly GlobalNode GLOBAL_NODE = new GlobalNode();
    public GlobalNode()
    {
        _scope.Type = BodyKind.GlobalBody;
    }

    public override SyntaxKind SyntaxKind => SyntaxKind.GlobalNode;

    public override Position Start { get; set; }
    public override Position End { get; set; }

    public override IdKind Id_type => IdKind.Identifier;

    public override bool IsScopeProvider => true;
    public override bool IsIdentifiable => false;

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


