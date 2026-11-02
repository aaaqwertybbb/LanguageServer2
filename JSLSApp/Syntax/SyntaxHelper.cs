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

    public static void RecursiveSearch(ref RecursiveSearch_Args args)
    {
        // Do not expect 'before' to stay the same later in this function because the variable is overwritten.
        ref var scope = ref args.ScopeProvider.GetScope();

        var i = 0;
        for (; i < scope.BodyList.Count; i++)
        {
            SyntaxNode? child_node = scope.BodyList[i];
            args.CurrentIndexPosition += child_node.DistanceFromPreviousNode;

            if (args.TargetIndexPosition >= args.CurrentIndexPosition && args.TargetIndexPosition <= args.CurrentIndexPosition + child_node.Length)
            {
                args.TargetNode = child_node;
                return;
            }
            else if (args.TargetIndexPosition < args.CurrentIndexPosition)
            {
                return;
            }
            else if (args.TargetIndexPosition > args.CurrentIndexPosition)
            {
                if (child_node.IsScopeProvider)
                {
                    args.ScopeProvider = child_node;
                    RecursiveSearch(ref args);
                }
            }
        }
    }

    // Two ref structs, one is the mover and the other is the function specific

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

        // TODO: 'thisFunctionInvocationIsAtAnAfter' is probably obsolete.
        var thisFunctionInvocationIsAtAnAfter = false;
        
        var i = 0;
        for (; i < scope.BodyList.Count; i++)
        {
            SyntaxNode? child_node = scope.BodyList[i];
            args.CurrentIndexPosition += child_node.DistanceFromPreviousNode;

            if (args.TargetIndexPosition >= args.CurrentIndexPosition && args.TargetIndexPosition <= args.CurrentIndexPosition + child_node.Length)
            {
                args.Among.Node = child_node;
                args.Among.PositionIndex = args.CurrentIndexPosition;
                i++;
                break;
            }
            else if (args.TargetIndexPosition < args.CurrentIndexPosition)
            {
                thisFunctionInvocationIsAtAnAfter = true;
                break;
            }
            else if (args.TargetIndexPosition > args.CurrentIndexPosition)
            {
                args.Before.Node = child_node;
                args.Before.PositionIndex = args.CurrentIndexPosition;

                if (child_node.IsScopeProvider)
                {
                    RecursiveSearch_FindNodesAdjacentToTheEdit(ref args);
                    if (args.After.Node is not null)
                        break;
                }
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
                    args.CurrentIndexPosition += args.After.Node.DistanceFromPreviousNode;
                }
                args.After.PositionIndex = args.CurrentIndexPosition;
            }
        }
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
                var variableDeclarationNode = (VariableDeclarationNode)node;
                if (nodeWithPosition.PositionIndex < chars.Length && nodeWithPosition.PositionIndex + variableDeclarationNode.Length <= chars.Length)
                {
                    return new string(chars.Slice(nodeWithPosition.PositionIndex, variableDeclarationNode.Length));
                }
                if (node.IsIdentifiable)
                {
                    return node.GetIdentifierHash().ToString();
                }
                return "VariableDeclarationNode";
                break;
            case SyntaxKind.VariableReferenceNode:
                var variableReferenceNode = (VariableReferenceNode)node;
                if (nodeWithPosition.PositionIndex < chars.Length && nodeWithPosition.PositionIndex + variableReferenceNode.Length <= chars.Length)
                {
                    return new string(chars.Slice(nodeWithPosition.PositionIndex, variableReferenceNode.Length));
                }
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
