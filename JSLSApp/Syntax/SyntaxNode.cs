using JSLSApp.LspTypes;
using System.Text;

namespace JSLSApp.Syntax;

/// <summary>
/// TODO: Relative positions so you can modify the tree without "updating every position"?
/// </summary>
public abstract class SyntaxNode
{
    public abstract SyntaxKind SyntaxKind { get; }
    /// <summary>
    /// TODO: Character or byte distance. I'm gonna start with character.
    /// 
    /// <see cref="Length"/>
    /// </summary>
    public abstract int DistanceFromPreviousNode { get; set; }
    /// <summary>
    /// TODO: Character or byte distance. I'm gonna start with character.
    /// 
    /// <see cref="DistanceFromPreviousNode"/>
    /// 
    /// TODO: Length might not make sense.
    /// 
    /// TODO: Length maybe could make sense if you tracked the version of the text as it gets edited...
    /// ...And maybe the utility of this would be that caching the length as you travel over nodes maybe makes traversal faster?...
    /// ...But maybe it is just not a good idea.
    /// </summary>
    public abstract int Length { get; set; }

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
    /// TODO: hash collisions by storing a special node that contains a list for the collisions and then you know it was a collision because you read the syntax kind? but then how do you determine the initial collision?
    /// 
    /// If <see cref="IsScopeProvider"/> is false, this will throw an exception.
    /// </summary>
    public virtual ref int GetIdentifierHash() => throw new InvalidOperationException("Not identifiable");
}
