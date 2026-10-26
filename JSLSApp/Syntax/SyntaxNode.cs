using JSLSApp.LspTypes;
using System.Text;

namespace JSLSApp.Syntax;

/// <summary>
/// TODO: Relative positions so you can modify the tree without "updating every position"?
/// </summary>
public abstract class SyntaxNode
{
    public abstract SyntaxKind SyntaxKind { get; }
    public abstract Position Start { get; set; }
    public abstract Position End { get; set; }

    /// <summary>
    /// I'm being a bit awkward with how strictly I'm following the example
    /// but I just need the first version to work.
    /// 
    /// As well the JSON may or may not be a simple default serialization.
    /// Based on the JSON it seems every node has a list of children... or maybe the body.
    /// But maybe this is all a shared list or etc... I have no idea.
    /// </summary>
    public abstract IdKind Id_type { get; }
    /// <summary>
    /// <see cref="GetScope"/>
    /// </summary>
    public abstract bool IsScopeProvider { get; }
    /// <summary>
    /// <see cref="GetIdentifierHash"/>
    /// </summary>
    public abstract bool IsIdentifiable { get; }

    /// <summary>
    /// If <see cref="IsScopeProvider"/> is false, this will throw an exception.
    /// </summary>
    public virtual ref Scope GetScope() => throw new InvalidOperationException("Not a scope provider");

    /// <summary>
    /// TODO: hash collisions amongst identifiers (not a keyword)?
    /// 
    /// If <see cref="IsScopeProvider"/> is false, this will throw an exception.
    /// </summary>
    public virtual ref int GetIdentifierHash() => throw new InvalidOperationException("Not identifiable");
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


