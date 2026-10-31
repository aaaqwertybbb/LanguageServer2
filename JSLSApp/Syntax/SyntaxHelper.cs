using JSLSApp.LspTypes;

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

    /// <summary>
    /// If you're going to invoke this: 'before.Node.IsScopeProvider' must be true.
    /// That is not to say the end resulting 'before.Node.IsScopeProvider' has to be true.
    /// 
    /// before.Node = scopeProvider;
    /// before.Position = new Position(currentIndexLine, currentIndexCharacter);
    /// before.PositionIndex = currentIndexPosition;
    /// </summary>
    public static void RecursiveSearch_FindNodesAdjacentToTheEdit(ref RecursiveSearch_FindNodesAdjacentToTheEdit_Args args)
    {
        // Do not expect 'before' to stay the same later in this function because the variable is overwritten.
        ref var scope = ref args.Before.Node.GetScope();

        var thisFunctionInvocationIsAtAnAfter = false;
        
        var i = 0;
        for (; i < scope.BodyList.Count; i++)
        {
            SyntaxNode? child_node = scope.BodyList[i];
            WalkPositionIndices_mutable_FindNodesAdjacentToTheEdit(ref args, child_node.DistanceFromPreviousNode);

            if (args.CurrentIndexLine < args.TargetIndexLine) // before line
            {
                args.Before.Node = child_node;
                args.Before.Position = new Position(args.CurrentIndexLine, args.CurrentIndexCharacter);
                args.Before.PositionIndex = args.CurrentIndexPosition;

                if (child_node.IsScopeProvider)
                {
                    ref var childScope = ref child_node.GetScope();
                    if (childScope.End.line >= args.TargetIndexLine && childScope.BodyList.Count > 0)
                    {
                        RecursiveSearch_FindNodesAdjacentToTheEdit(ref args);
                        i++;
                        break;
                    }
                    else
                    {
                        SkipBranchButStillCountItPositionWise_FindNodesAdjacentToTheEdit(ref args, child_node);
                    }
                }
            }
            else if (args.CurrentIndexLine > args.TargetIndexLine) // after line
            {
                thisFunctionInvocationIsAtAnAfter = true;
                break;
            }
            else // same line
            {
                if (args.CurrentIndexCharacter > args.TargetIndexCharacter) // after character
                {
                    thisFunctionInvocationIsAtAnAfter = true;
                    break;
                }

                var found = false;

                if (args.TargetIndexCharacter == args.CurrentIndexCharacter)
                {
                    found = true;
                }
                else
                {
                    // TODO: Awkward assumption that the childNode's `startLine` and `endLine` would be equal. (currently only the length of a node's "word based identifier" is being accounted for, so this is currently true.
                    (_, _, var childNode_endCharacter) = WalkPositionIndices_return_FindNodesAdjacentToTheEdit(ref args, child_node.Length);
                    if (childNode_endCharacter >= args.TargetIndexCharacter)
                    {
                        found = true;
                    }
                    else
                    {
                        args.Before.Node = child_node;
                        args.Before.Position = new Position(args.CurrentIndexLine, args.CurrentIndexCharacter);
                        args.Before.PositionIndex = args.CurrentIndexPosition;
                    }
                }

                if (!found)
                    continue;

                args.Among.Node = child_node;
                args.Among.Position = new Position(args.CurrentIndexLine, args.CurrentIndexCharacter);
                args.Among.PositionIndex = args.CurrentIndexPosition;
                i++;
                break;
            }
        }

        // Found 'among' node: then 'i' is ensured to have been incremented prior to the break.
        //
        // All nodes are before: 'i' will be >= scope.BodyList.Count.
        //
        // Found 'after' node: then 'i' will NOT have been incremented prior to the break.
        //
        if (i < scope.BodyList.Count)
        {
            if (args.After.Node is null)
            {
                args.After.Node = scope.BodyList[i];
                if (!thisFunctionInvocationIsAtAnAfter)
                {
                    // i was incremented then 'break' but you still need to walk to the node.
                    WalkPositionIndices_mutable_FindNodesAdjacentToTheEdit(ref args, args.After.Node.DistanceFromPreviousNode);
                }
                args.After.Position = new(args.CurrentIndexLine, args.CurrentIndexCharacter);
                args.After.PositionIndex = args.CurrentIndexPosition;
            }
        }
    }

    public static void WalkPositionIndices_mutable_FindNodesAdjacentToTheEdit(ref RecursiveSearch_FindNodesAdjacentToTheEdit_Args args, int distance)
    {
        if (args.CurrentIndexPosition >= args.Chars.Length)
            return;

        if (args.CurrentIndexPosition + distance >= args.Chars.Length)
            distance = args.Chars.Length - args.CurrentIndexPosition;

        for (var i = 0; i < distance; i++)
        {
            switch (args.Chars[args.CurrentIndexPosition])
            {
                //case '\r': TODO: Include or not '\r'? When using my own editor I swap everything to '\n' but...
                case '\n':
                    args.CurrentIndexLine++;
                    args.CurrentIndexCharacter = 0;
                    break;
                default:
                    args.CurrentIndexCharacter++;
                    break;
            }
            args.CurrentIndexPosition++;
        }
    }

    public static (int CurrentIndexPosition, int CurrentIndexLine, int CurrentIndexCharacter) WalkPositionIndices_return_FindNodesAdjacentToTheEdit(ref RecursiveSearch_FindNodesAdjacentToTheEdit_Args args, int distance)
    {
        if (args.CurrentIndexPosition >= args.Chars.Length)
            return (args.CurrentIndexPosition, args.CurrentIndexLine, args.CurrentIndexCharacter);

        if (args.CurrentIndexPosition + distance >= args.Chars.Length)
            distance = args.Chars.Length - args.CurrentIndexPosition;

        for (var i = 0; i < distance; i++)
        {
            switch (args.Chars[args.CurrentIndexPosition])
            {
                //case '\r': TODO: Include or not '\r'? When using my own editor I swap everything to '\n' but...
                case '\n':
                    args.CurrentIndexLine++;
                    args.CurrentIndexCharacter = 0;
                    break;
                default:
                    args.CurrentIndexCharacter++;
                    break;
            }
            args.CurrentIndexPosition++;
        }

        return (args.CurrentIndexPosition, args.CurrentIndexLine, args.CurrentIndexCharacter);
    }

    public static void SkipBranchButStillCountItPositionWise_FindNodesAdjacentToTheEdit(ref RecursiveSearch_FindNodesAdjacentToTheEdit_Args args, SyntaxNode? targetScopeProvider)
    {
        var isNaturalHandOff = false;
        var i = 0;
        while (targetScopeProvider is not null)
        {
            args.Before.Node = targetScopeProvider;
            args.Before.Position = new(args.CurrentIndexLine, args.CurrentIndexCharacter);
            args.Before.PositionIndex = args.CurrentIndexPosition;
            ref var scope = ref targetScopeProvider.GetScope();
            for (; i < scope.BodyList.Count; i++)
            {
                var childNode = scope.BodyList[i];
                args.Before.Node = childNode;
                if (args.CurrentIndexPosition < args.Chars.Length)
                {
                    var distance = childNode.DistanceFromPreviousNode;
                    if (args.CurrentIndexPosition + distance >= args.Chars.Length)
                        distance = args.Chars.Length - args.CurrentIndexPosition;

                    for (var j = 0; j < distance; j++)
                    {
                        switch (args.Chars[args.CurrentIndexPosition])
                        {
                            //case '\r': TODO: Include or not '\r'? When using my own editor I swap everything to '\n' but...
                            case '\n':
                                args.CurrentIndexLine++;
                                args.CurrentIndexCharacter = 0;
                                break;
                            default:
                                args.CurrentIndexCharacter++;
                                break;
                        }
                        args.CurrentIndexPosition++;
                    }
                }
                args.Before.Position = new(args.CurrentIndexLine, args.CurrentIndexCharacter);
                args.Before.PositionIndex = args.CurrentIndexPosition;
                if (childNode.IsScopeProvider)
                {
                    args.ReturnStack.Push((targetScopeProvider, i));
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
                if (args.ReturnStack.Count > 0)
                {
                    var returnEntry = args.ReturnStack.Pop();
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
            SyntaxKind.IfStatementNode => "IfStatementNode",
            SyntaxKind.FunctionDeclarationNode => "FunctionDeclarationNode",
            SyntaxKind.GlobalNode => "GlobalNode",
            SyntaxKind.VariableDeclarationNode => "VariableDeclarationNode",
            SyntaxKind.VariableReferenceNode => "VariableReferenceNode",
            SyntaxKind.None => "None",
            _ => $"GetSyntaxKindString is missing a map for {syntaxKind.ToString()}"
        };
    }
    
    public static string GetIdentifierText(SyntaxNodeWithPosition nodeWithPosition, ReadOnlySpan<char> chars)
    {
        var node = nodeWithPosition.Node;
        if (node is null)
        {
            return "null";
        }
        switch (node.SyntaxKind)
        {
            case SyntaxKind.AmpersandToken:
                if (node.IsIdentifiable)
                {
                    return node.GetIdentifierHash().ToString();
                }
                return "AmpersandToken";
                break;
            case SyntaxKind.BlockNode:
                if (node.IsIdentifiable)
                {
                    return node.GetIdentifierHash().ToString();
                }
                return "BlockNode";
                break;
            case SyntaxKind.CatchKeywordToken:
                if (node.IsIdentifiable)
                {
                    return node.GetIdentifierHash().ToString();
                }
                return "CatchKeywordToken";
                break;
            case SyntaxKind.IfStatementNode:
                if (node.IsIdentifiable)
                {
                    return node.GetIdentifierHash().ToString();
                }
                return "IfStatementNode";
                break;
            case SyntaxKind.FunctionDeclarationNode:
                var functionDeclarationNode = (FunctionDeclarationNode)node;
                if (nodeWithPosition.PositionIndex < chars.Length && nodeWithPosition.PositionIndex + functionDeclarationNode.Length <= chars.Length)
                {
                    return new string(chars.Slice(nodeWithPosition.PositionIndex, functionDeclarationNode.Length));
                }
                if (node.IsIdentifiable)
                {
                    return node.GetIdentifierHash().ToString();
                }
                return "FunctionDeclarationNode";
                break;
            case SyntaxKind.GlobalNode:
                if (node.IsIdentifiable)
                {
                    return node.GetIdentifierHash().ToString();
                }
                return "GlobalNode";
                break;
            case SyntaxKind.VariableDeclarationNode:
                if (node.IsIdentifiable)
                {
                    return node.GetIdentifierHash().ToString();
                }
                return "VariableDeclarationNode";
                break;
            case SyntaxKind.VariableReferenceNode:
                if (node.IsIdentifiable)
                {
                    return node.GetIdentifierHash().ToString();
                }
                return "VariableReferenceNode";
                break;
            case SyntaxKind.None:
                if (node.IsIdentifiable)
                {
                    return node.GetIdentifierHash().ToString();
                }
                return "None";
                break;
            default:
                if (node.IsIdentifiable)
                {
                    return node.GetIdentifierHash().ToString();
                }
                return $"GetIdentifierText is missing a map for {node.SyntaxKind.ToString()}";
                break;
        }
        
    }
}
