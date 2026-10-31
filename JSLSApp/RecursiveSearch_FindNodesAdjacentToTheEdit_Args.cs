using JSLSApp.LspTypes;
using JSLSApp.Syntax;
using System.Runtime.InteropServices;

namespace JSLSApp;

internal ref struct RecursiveSearch_FindNodesAdjacentToTheEdit_Args
{
    public RecursiveSearch_FindNodesAdjacentToTheEdit_Args(JavaScriptDocument javaScriptDocument, int targetIndexLine, int targetIndexCharacter, Stack<SyntaxNode> inclusiveFinalNodeToConsiderAtEachDepth, Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> returnStack, ref SyntaxNodeWithPosition before, ref SyntaxNodeWithPosition among, ref SyntaxNodeWithPosition after)
    {
        Before = ref before;
        Among = ref among;
        After = ref after;

        InclusiveFinalNodeToConsiderAtEachDepth = inclusiveFinalNodeToConsiderAtEachDepth;
        InclusiveFinalNodeToConsiderAtEachDepth.Clear();

        ReturnStack = returnStack;
        ReturnStack.Clear();

        CurrentIndexPosition = 0;
        CurrentIndexLine = 0;
        CurrentIndexCharacter = 0;

        Before.Node = javaScriptDocument.CompilationUnit.GlobalNode;
        Before.Position = new Position(CurrentIndexLine, CurrentIndexCharacter);
        Before.PositionIndex = CurrentIndexPosition;

        TargetIndexLine = targetIndexLine;
        TargetIndexCharacter = targetIndexCharacter;

        Chars = CollectionsMarshal.AsSpan(javaScriptDocument.Chars);
    }

    internal ref SyntaxNodeWithPosition Before;
    internal ref SyntaxNodeWithPosition Among;
    internal ref SyntaxNodeWithPosition After;

    internal Stack<SyntaxNode> InclusiveFinalNodeToConsiderAtEachDepth;
    internal Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> ReturnStack;

    internal int CurrentIndexPosition;
    internal int CurrentIndexLine;
    internal int CurrentIndexCharacter;

    internal int TargetIndexLine;
    internal int TargetIndexCharacter;

    internal ReadOnlySpan<char> Chars;
}
