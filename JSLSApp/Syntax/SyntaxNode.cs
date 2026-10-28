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
    /// </summary>
    public abstract int Length { get; set; }

    /// <summary>
    /// I'm being a bit awkward with how strictly I'm following the example
    /// but I just need the first version to work.
    /// 
    /// As well the JSON may or may not be a simple default serialization.
    /// Based on the JSON it seems every node has a list of children... or maybe the body.
    /// But maybe this is all a shared list or etc... I have no idea.
    /// 
    /// TODO: Remove this 'Id_type' property.
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
    /// TODO: hash collisions by storing a special node that contains a list for the collisions and then you know it was a collision because you read the syntax kind? but then how do you determine the initial collision?
    /// 
    /// If <see cref="IsScopeProvider"/> is false, this will throw an exception.
    /// </summary>
    public virtual ref int GetIdentifierHash() => throw new InvalidOperationException("Not identifiable");
}
