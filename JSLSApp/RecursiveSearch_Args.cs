using JSLSApp.LspTypes;
using JSLSApp.Syntax;
using System.Runtime.InteropServices;

namespace JSLSApp;

internal ref struct RecursiveSearch_Args
{
    public RecursiveSearch_Args(JavaScriptDocument javaScriptDocument, int targetIndexLine, int targetIndexCharacter, Stack<SyntaxNode> inclusiveFinalNodeToConsiderAtEachDepth, Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> returnStack)
    {
        InclusiveFinalNodeToConsiderAtEachDepth = inclusiveFinalNodeToConsiderAtEachDepth;
        InclusiveFinalNodeToConsiderAtEachDepth.Clear();

        ReturnStack = returnStack;
        ReturnStack.Clear();

        CurrentIndexPosition = 0;
        CurrentIndexLine = 0;
        CurrentIndexCharacter = 0;

        ScopeProvider = javaScriptDocument.CompilationUnit.GlobalNode;

        TargetIndexLine = targetIndexLine;
        TargetIndexCharacter = targetIndexCharacter;

        Chars = CollectionsMarshal.AsSpan(javaScriptDocument.Chars);
    }

    // Contains both IsScopeProvider==true nodes and IsScopeProvider==false nodes
    // BECAUSE it contains the definition node itself as the first thing that gets pushed.
    // Other than that first pushed node everything is IsScopeProvider==true.
    internal Stack<SyntaxNode> InclusiveFinalNodeToConsiderAtEachDepth;
    internal Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> ReturnStack;

    internal int TotalChecks;

    internal int CurrentIndexPosition;
    internal int CurrentIndexLine;
    internal int CurrentIndexCharacter;

    internal SyntaxNode ScopeProvider;

    internal int TargetIndexLine;
    internal int TargetIndexCharacter;

    internal SyntaxNode? TargetNode;
    internal Position TargetNodePosition;
    internal int TargetNodePositionIndex;

    internal ReadOnlySpan<char> Chars;
}
