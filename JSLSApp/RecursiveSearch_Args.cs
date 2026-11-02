using JSLSApp.LspTypes;
using JSLSApp.Syntax;
using System.Runtime.InteropServices;

namespace JSLSApp;

internal ref struct RecursiveSearch_Args
{
    public RecursiveSearch_Args(JavaScriptDocument javaScriptDocument, int targetIndexPosition, Stack<SyntaxNode> inclusiveFinalNodeToConsiderAtEachDepth, Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> returnStack)
    {
        InclusiveFinalNodeToConsiderAtEachDepth = inclusiveFinalNodeToConsiderAtEachDepth;
        InclusiveFinalNodeToConsiderAtEachDepth.Clear();

        ReturnStack = returnStack;
        ReturnStack.Clear();

        CurrentIndexPosition = 0;

        ScopeProvider = javaScriptDocument.CompilationUnit.GlobalNode;

        TargetIndexPosition = targetIndexPosition;

        Chars = CollectionsMarshal.AsSpan(javaScriptDocument.Chars);
    }

    // Contains both IsScopeProvider==true nodes and IsScopeProvider==false nodes
    // BECAUSE it contains the definition node itself as the first thing that gets pushed.
    // Other than that first pushed node everything is IsScopeProvider==true.
    internal Stack<SyntaxNode> InclusiveFinalNodeToConsiderAtEachDepth;
    internal Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> ReturnStack;

    internal int TotalChecks;

    internal int CurrentIndexPosition;

    internal SyntaxNode ScopeProvider;

    internal int TargetIndexPosition;

    internal SyntaxNode? TargetNode;
    internal Position TargetNodePosition;

    internal ReadOnlySpan<char> Chars;
}
