using JSLSApp.LspTypes;
using JSLSApp.Syntax;

namespace JSLSApp;

internal ref struct RecursiveSearch_Args
{
    public RecursiveSearch_Args(JavaScriptDocument javaScriptDocument,
        int targetIndexPosition,
        Stack<SyntaxNode> inclusiveFinalNodeToConsiderAtEachDepth,
        Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings, bool IsAtAnAfter)> returnStack)
    {
        InclusiveFinalNodeToConsiderAtEachDepth = inclusiveFinalNodeToConsiderAtEachDepth;
        InclusiveFinalNodeToConsiderAtEachDepth.Clear();

        ReturnStack = returnStack;
        ReturnStack.Clear();

        CurrentIndexPosition = 0;

        ScopeProvider = javaScriptDocument.CompilationUnit.GlobalNode;

        TargetIndexPosition = targetIndexPosition;

        Chars = new ReadOnlySpan<byte>(javaScriptDocument.Chars, 0, javaScriptDocument.Count);
    }

    // Contains both IsScopeProvider==true nodes and IsScopeProvider==false nodes
    // BECAUSE it contains the definition node itself as the first thing that gets pushed.
    // Other than that first pushed node everything is IsScopeProvider==true.
    internal Stack<SyntaxNode> InclusiveFinalNodeToConsiderAtEachDepth;
    /// <summary>
    /// IsAtAnAfter is being ignored in this function, but the same Stack object is being pooled from a scenario where IsAtAnAfter is necessary.
    /// </summary>
    /// <remarks>
    /// As of writing this 'remarks' the only scenario where IsAtAnAfter is necessary is for tracking the 'before', 'among', and 'after' nodes.
    /// The GC overhead of a separate app level Stack solely for one less variable in the value tuple is certainly not worthwhile.
    /// Especially because size of this stack is arbitrary -- based on the nesting of the code being parsed --, and thus
    /// may or may not be a "large object".
    /// </remarks>
    internal Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings, bool IsAtAnAfter)> ReturnStack;

    internal int TotalChecks;

    internal int CurrentIndexPosition;

    internal SyntaxNode ScopeProvider;

    internal int TargetIndexPosition;

    internal SyntaxNode? TargetNode;
    internal Position TargetNodePosition;

    internal ReadOnlySpan<byte> Chars;
}
