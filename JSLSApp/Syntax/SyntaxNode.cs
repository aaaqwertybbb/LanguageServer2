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
    /// 
    /// TODO: Rename this to something more accurately meaning: "Distance from the start of the previous node".
    /// 
    /// TODO: Maybe you'd prefer "distance from the end of the previous node". Either way, I am making the executive decision to "just start" and I'll figure this out later for the sake of "anxiety"/"procrastination" issues.
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
    /// 
    /// Length does make sense actually, it is the length of the identifier (or whatever you want it to be).
    /// Something more complicated like the length of a scope you can probably keep 0 to indicate that it can't be known ahead of time or something?
    /// "can't be known ahead of time" meaning it is too frequently changing and causes too much overhead to maintain the value for.
    /// More accurately: it is changing as a result of some other node having changed and this propagation of change is the "too much overhead".
    /// But maybe you could do it for this because you're talking about "just" each member of the breadcrumb trail being updated rather than
    /// relative to the entire file.
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
