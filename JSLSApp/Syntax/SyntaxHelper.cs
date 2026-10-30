namespace JSLSApp.Syntax;

internal static class SyntaxHelper
{
    public static SyntaxNode? RecursiveSearch_BodyOnly(SyntaxNode scopeProvider, int indexLine, ref int totalChecks, ref int nodeCount)
    {
        return null;

        ref var scope = ref scopeProvider.GetScope();

        nodeCount += scope.CountOfChildrenThatAreDefinitions;

        if (scope.CountOfChildrenThatAreDefinitions == 0)
        {
            return null;
        }

        foreach (var child_node in scope.BodyList)
        {
            if (!child_node.IsScopeProvider)
            {
                continue;
            }

            ref var childScope = ref child_node.GetScope();

            if (childScope.Type == BodyKind.EXTERNAL_FunctionBody ||
                (childScope.Start.line > indexLine || childScope.End.line < indexLine))
            {
                continue;
            }

            totalChecks++;
            // // TODO: relative changes break this;
            //if (child_node.Start.line == indexLine)
            //{
            //    nodeCount += childScope.CountOfChildrenThatAreDefinitions;
            //    return child_node;
            //}
            // super arbitrary 1,000 limit until I sleep on the logic.
            // it should do a tree walking -ish algorithm so 1k isn't too bad?
            if (totalChecks > 1000)
            {
                return null;
            }

            var nodeThatWasCloser = RecursiveSearch_BodyOnly(child_node, indexLine, ref totalChecks, ref nodeCount);
            if (nodeThatWasCloser is null)
            {
                return child_node;
            }
            else
            {
                return nodeThatWasCloser;
            }
        }
        return null;
    }

    /// <summary>
    /// TODO: 'RecursiveSearchArgs' object instance to reduce parameter overhead from inner recursive invocations.
    /// </summary>
    public static SyntaxNode? RecursiveSearch(List<char> chars, SyntaxNode scopeProvider, int targetIndexLine, int targetIndexCharacter, ref int totalChecks, ref int currentIndexPosition, ref int currentIndexLine, ref int currentIndexCharacter, Stack<SyntaxNode> inclusiveFinalNodeToConsiderAtEachDepth, Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> returnStack)
    {
        ref var scope = ref scopeProvider.GetScope();

        var parentNode_startLine = currentIndexLine;
        var parentNode_startCharacter = currentIndexCharacter;
        //var parentNode_endLine;
        //var parentNode_endCharacter;

        for (int i = 0; i < scope.BodyList.Count; i++)
        {
            SyntaxNode? child_node = scope.BodyList[i];
            WalkPositionIndices_mutable(chars, child_node.DistanceFromPreviousNode, ref currentIndexPosition, ref currentIndexLine, ref currentIndexCharacter);
            var childNode_startLine = currentIndexLine;
            var childNode_startCharacter = currentIndexCharacter;
            (_, var childNode_endLine, var childNode_endCharacter) = WalkPositionIndices_return(chars, child_node.Length, currentIndexPosition, currentIndexLine, currentIndexCharacter);

            if (child_node.IsScopeProvider && child_node.GetScope().Type == BodyKind.EXTERNAL_FunctionBody)
            {
                continue;
            }
            totalChecks++;
            
            if (childNode_startLine == targetIndexLine)
            {
                // TODO: This might be inaccurate for a function definition (or etc...) that spans a single line; if you tried to target an identifier in the code block it might target the function definition I'm not sure.
                if (childNode_startCharacter <= targetIndexCharacter && childNode_endCharacter >= targetIndexCharacter)
                {
                    return child_node;
                }
            }
            // super arbitrary 1,000 limit until I sleep on the logic.
            // it should do a tree walking -ish algorithm so 1k isn't too bad?
            if (totalChecks > 1000)
            {
                return null;
            }

            if (!child_node.IsScopeProvider)
            {
                continue;
            }

            ref var childScope = ref child_node.GetScope();
            if (childScope.End.line >= targetIndexLine)
            {
                if (childScope.BodyList.Count > 0)
                {
                    var node = RecursiveSearch(chars, child_node, targetIndexLine, targetIndexCharacter, ref totalChecks, ref currentIndexPosition, ref currentIndexLine, ref currentIndexCharacter, inclusiveFinalNodeToConsiderAtEachDepth, returnStack);
                    if (node is not null)
                    {
                        return node;
                    }
                }
                break;
            }
            else
            {
                SkipBranchButStillCountItPositionWise(chars, child_node, ref currentIndexPosition, ref currentIndexLine, ref currentIndexCharacter, inclusiveFinalNodeToConsiderAtEachDepth, returnStack);
            }
        }
        return null;
    }
    
    public static void RecursiveSearch_FindNodesAdjacentToTheEdit(List<char> chars, SyntaxNode scopeProvider, int targetIndexLine, int targetIndexCharacter, ref int currentIndexPosition, ref int currentIndexLine, ref int currentIndexCharacter, ref SyntaxNode? before, ref SyntaxNode? among, ref SyntaxNode? after, Stack<SyntaxNode> inclusiveFinalNodeToConsiderAtEachDepth, Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> returnStack)
    {
        // when you descend you need to update the before of the parent recursive invocation.
        // this is a result of how I have things written where a scopeProvider, aaa you need 'scopeProvider.deepestRightNode' -> aaa
        var myBefore = scopeProvider;
        ref var scope = ref scopeProvider.GetScope();

        var parentNode_startLine = currentIndexLine;
        var parentNode_startCharacter = currentIndexCharacter;
        //var parentNode_endLine;
        //var parentNode_endCharacter;

        for (int i = 0; i < scope.BodyList.Count; i++)
        {
            SyntaxNode? child_node = scope.BodyList[i];
            WalkPositionIndices_mutable(chars, child_node.DistanceFromPreviousNode, ref currentIndexPosition, ref currentIndexLine, ref currentIndexCharacter);
            var childNode_startLine = currentIndexLine;
            var childNode_startCharacter = currentIndexCharacter;
            (_, var childNode_endLine, var childNode_endCharacter) = WalkPositionIndices_return(chars, child_node.Length, currentIndexPosition, currentIndexLine, currentIndexCharacter);

            if (child_node.IsScopeProvider && child_node.GetScope().Type == BodyKind.EXTERNAL_FunctionBody)
            {
                continue;
            }
            
            if (childNode_startLine == targetIndexLine)
            {
                // TODO: This might be inaccurate for a function definition (or etc...) that spans a single line; if you tried to target an identifier in the code block it might target the function definition I'm not sure.
                if (childNode_startCharacter <= targetIndexCharacter && childNode_endCharacter >= targetIndexCharacter)
                {
                    before = myBefore;
                    among = child_node;
                    if (i < scope.BodyList.Count - 1)
                    {
                        after = scope.BodyList[i + 1];
                    }
                    else if (child_node.IsScopeProvider && child_node.GetScope().BodyList.Count > 0)
                    {
                        after = child_node.GetScope().BodyList[0];
                    }
                    else
                    {
                        after = null;
                    }
                    
                    return;
                }
            }

            if (!child_node.IsScopeProvider)
            {
                before = myBefore;
                continue;
            }

            ref var childScope = ref child_node.GetScope();
            if (childScope.End.line >= targetIndexLine)
            {
                if (childScope.BodyList.Count > 0)
                {
                    before = myBefore;
                    RecursiveSearch_FindNodesAdjacentToTheEdit(chars, child_node, targetIndexLine, targetIndexCharacter, ref currentIndexPosition, ref currentIndexLine, ref currentIndexCharacter, ref before, ref among, ref after, inclusiveFinalNodeToConsiderAtEachDepth, returnStack);
                    if (among is not null)
                    {
                        return;
                    }
                }
                break;
            }
            else
            {
                SkipBranchButStillCountItPositionWise(chars, child_node, ref currentIndexPosition, ref currentIndexLine, ref currentIndexCharacter, inclusiveFinalNodeToConsiderAtEachDepth, returnStack);
            }
        }
    }

    public static void SkipBranchButStillCountItPositionWise(List<char> chars, SyntaxNode? targetScopeProvider, ref int currentIndexPosition, ref int currentIndexLine, ref int currentIndexCharacter, Stack<SyntaxNode> inclusiveFinalNodeToConsiderAtEachDepth, Stack<(SyntaxNode ScopeProvider, int ChildIndexOfScopeProviderAmongItsSiblings)> returnStack)
    {
        var isNaturalHandOff = false;
        var i = 0;
        while (targetScopeProvider is not null)
        {
            ref var scope = ref targetScopeProvider.GetScope();
            for (; i < scope.BodyList.Count; i++)
            {
                var childNode = scope.BodyList[i];
                if (currentIndexPosition < chars.Count)
                {
                    var distance = childNode.DistanceFromPreviousNode;
                    if (currentIndexPosition + distance >= chars.Count)
                        distance = chars.Count - currentIndexPosition;

                    for (var j = 0; j < distance; j++)
                    {
                        switch (chars[currentIndexPosition])
                        {
                            //case '\r': TODO: Include or not '\r'? When using my own editor I swap everything to '\n' but...
                            case '\n':
                                currentIndexLine++;
                                currentIndexCharacter = 0;
                                break;
                            default:
                                currentIndexCharacter++;
                                break;
                        }
                        currentIndexPosition++;
                    }
                }
                if (childNode.IsScopeProvider)
                {
                    returnStack.Push((targetScopeProvider, i));
                    targetScopeProvider = childNode;
                    isNaturalHandOff = true;
                    break;
                }
            }
            if (isNaturalHandOff)
            {
                isNaturalHandOff = false;
                i = 0;
            }
            else
            {
                if (returnStack.Count > 0)
                {
                    var returnEntry = returnStack.Pop();
                    targetScopeProvider = returnEntry.ScopeProvider;
                    i = returnEntry.ChildIndexOfScopeProviderAmongItsSiblings + 1;
                }
                else
                {
                    targetScopeProvider = null;
                }
            }
        }
    }

    public static void WalkPositionIndices_mutable(List<char> chars, int distance, ref int currentIndexPosition, ref int currentIndexLine, ref int currentIndexCharacter)
    {
        if (currentIndexPosition >= chars.Count)
            return;

        if (currentIndexPosition + distance >= chars.Count)
            distance = chars.Count - currentIndexPosition;

        for (var i = 0; i < distance; i++)
        {
            switch (chars[currentIndexPosition])
            {
                //case '\r': TODO: Include or not '\r'? When using my own editor I swap everything to '\n' but...
                case '\n':
                    currentIndexLine++;
                    currentIndexCharacter = 0;
                    break;
                default:
                    currentIndexCharacter++;
                    break;
            }
            currentIndexPosition++;
        }
    }

    public static (int CurrentIndexPosition, int CurrentIndexLine, int CurrentIndexCharacter) WalkPositionIndices_return(List<char> chars, int distance, int currentIndexPosition, int currentIndexLine, int currentIndexCharacter)
    {
        if (currentIndexPosition >= chars.Count)
            return (currentIndexPosition, currentIndexLine, currentIndexCharacter);

        if (currentIndexPosition + distance >= chars.Count)
            distance = chars.Count - currentIndexPosition;

        for (var i = 0; i < distance; i++)
        {
            switch (chars[currentIndexPosition])
            {
                //case '\r': TODO: Include or not '\r'? When using my own editor I swap everything to '\n' but...
                case '\n':
                    currentIndexLine++;
                    currentIndexCharacter = 0;
                    break;
                default:
                    currentIndexCharacter++;
                    break;
            }
            currentIndexPosition++;
        }

        return (currentIndexPosition, currentIndexLine, currentIndexCharacter);
    }

    public static string GetSyntaxKindString(SyntaxKind syntaxKind)
    {
        return syntaxKind switch
        {
            SyntaxKind.AmpersandToken => "AmpersandToken",
            SyntaxKind.BlockNode => "BlockNode",
            SyntaxKind.CatchKeywordToken => "CatchKeywordToken",
            _ => "GetSyntaxKindString is missing a map for some token"
        };
    }
}
