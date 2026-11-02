using JSLSApp.LspTypes;
using JSLSApp.Syntax;
using System.Runtime.InteropServices;

namespace JSLSApp;

internal ref struct RecursiveSearch_FindNodesAdjacentToTheEdit_Args
{
    public RecursiveSearch_FindNodesAdjacentToTheEdit_Args(JavaScriptDocument javaScriptDocument, int targetIndexPosition, Stack<SyntaxNode> inclusiveFinalNodeToConsiderAtEachDepth, Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> returnStack, ref SyntaxNodeWithPosition before, ref SyntaxNodeWithPosition among, ref SyntaxNodeWithPosition after)
    {
        Before = ref before;
        Among = ref among;
        After = ref after;

        InclusiveFinalNodeToConsiderAtEachDepth = inclusiveFinalNodeToConsiderAtEachDepth;
        InclusiveFinalNodeToConsiderAtEachDepth.Clear();

        ReturnStack = returnStack;
        ReturnStack.Clear();

        CurrentIndexPosition = 0;

        Before.Node = javaScriptDocument.CompilationUnit.GlobalNode;
        //Before.Position = new Position(CurrentIndexLine, CurrentIndexCharacter);
        Before.PositionIndex = CurrentIndexPosition;

        TargetIndexPosition = targetIndexPosition;

        Chars = new ReadOnlySpan<byte>(javaScriptDocument.Chars, 0, javaScriptDocument.Count);
    }

    internal ref SyntaxNodeWithPosition Before;
    internal ref SyntaxNodeWithPosition Among;
    internal ref SyntaxNodeWithPosition After;

    internal Stack<SyntaxNode> InclusiveFinalNodeToConsiderAtEachDepth;
    internal Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> ReturnStack;

    internal int CurrentIndexPosition;

    internal int TargetIndexPosition;

    internal ReadOnlySpan<byte> Chars;
}
