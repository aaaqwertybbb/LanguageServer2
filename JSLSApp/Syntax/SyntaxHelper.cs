using JSLSApp.LspTypes;
using System.Text;

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
    /// Internally is implemented as a 'while' loop.
    /// </summary>
    public static void RecursiveSearch(ref RecursiveSearch_Args args)
    {
        ref var scope = ref args.ScopeProvider.GetScope();
        var i = 0;

        while (true)
        {
            bool descended = false;

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
                        args.ReturnStack.Push((args.ScopeProvider, i + 1, false));
                        args.ScopeProvider = child_node;
                        scope = ref args.ScopeProvider.GetScope();
                        i = 0;
                        descended = true;
                        break;
                    }
                }
            }

            if (!descended)
            {
                if (args.ReturnStack.Count > 0)
                {
                    var parentContext = args.ReturnStack.Pop();
                    args.ScopeProvider = parentContext.ScopeProvider;
                    scope = ref args.ScopeProvider.GetScope();
                    i = parentContext.ChildIndexOfScopeProviderAmongItsSiblings;
                }
                else
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Internally is implemented as a 'while' loop.
    /// 
    /// If you're going to invoke this: 'before.Node.IsScopeProvider' must be true.
    /// That is not to say the end resulting 'before.Node.IsScopeProvider' has to be true.
    /// 
    /// before.Node = scopeProvider;
    /// before.Position = new Position(currentIndexLine, currentIndexCharacter);
    /// before.PositionIndex = currentIndexPosition;
    /// </summary>
    public static void RecursiveSearch_FindNodesAdjacentToTheEdit(ref RecursiveSearch_FindNodesAdjacentToTheEdit_Args args)
    {
        var scopeProvider = args.Before.Node;
        ref var scope = ref scopeProvider.GetScope();
        var i = 0;
        var thisFunctionInvocationIsAtAnAfter = false;

        while (true)
        {
            bool descended = false;

            for (; i < scope.BodyList.Count; i++)
            {
                SyntaxNode? child_node = scope.BodyList[i];
                args.CurrentIndexPosition += child_node.DistanceFromPreviousNode;

                if (args.TargetIndexPosition >= args.CurrentIndexPosition && args.TargetIndexPosition <= args.CurrentIndexPosition + child_node.Length)
                {
                    args.Among.Node = child_node;
                    args.Among.PositionIndex = args.CurrentIndexPosition;
                    i++;
                    break; // Break the for-loop to run the trailing 'After' detection logic
                }
                else if (args.TargetIndexPosition < args.CurrentIndexPosition)
                {
                    thisFunctionInvocationIsAtAnAfter = true;
                    break; // Break the for-loop to run the trailing 'After' detection logic
                }
                else if (args.TargetIndexPosition > args.CurrentIndexPosition)
                {
                    args.Before.Node = child_node;
                    args.Before.PositionIndex = args.CurrentIndexPosition;

                    if (child_node.IsScopeProvider)
                    {
                        // 1. Push the CAPTURED parent state onto the stack
                        args.ReturnStack.Push((scopeProvider, i, thisFunctionInvocationIsAtAnAfter));

                        // 2. Descend into the child scope context
                        scopeProvider = child_node;
                        scope = ref scopeProvider.GetScope();

                        // 3. Reset state variables for the nested scope
                        i = 0;
                        thisFunctionInvocationIsAtAnAfter = false;
                        descended = true;
                        break;
                    }
                }
            }

            // Skip the epilogue if we just cleanly moved downwards into a child scope
            if (descended)
            {
                continue;
            }

            // --- Epilogue Logic (Executed when a loop terminates or breaks out) ---
            if (i < scope.BodyList.Count)
            {
                if (args.After.Node is null)
                {
                    args.After.Node = scope.BodyList[i];
                    if (!thisFunctionInvocationIsAtAnAfter)
                    {
                        args.CurrentIndexPosition += args.After.Node.DistanceFromPreviousNode;
                    }
                    args.After.PositionIndex = args.CurrentIndexPosition;
                }
            }

            // Check if we have an After node. If so, we are entirely done.
            if (args.After.Node is not null)
            {
                return;
            }

            // If the current scope is fully exhausted and no After node was assigned, pop back up
            if (args.ReturnStack.Count > 0)
            {
                var parentContext = args.ReturnStack.Pop();

                //// Re-inflate the parent loop's state
                scopeProvider = parentContext.ScopeProvider;
                scope = ref scopeProvider.GetScope();

                i = parentContext.ChildIndexOfScopeProviderAmongItsSiblings;
                thisFunctionInvocationIsAtAnAfter = parentContext.IsAtAnAfter;

                // Because your original code had:
                // if (args.After.Node is not null) break;
                // immediately after the recursive call, checking it above handles this perfectly.
                // We just advance the parent loop by one increment step now.
                i++;
            }
            else
            {
                // Stack is empty and we finished processing the root scope
                return;
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
    
    public static string GetIdentifierText(SyntaxNodeWithPosition nodeWithPosition, ReadOnlySpan<byte> chars)
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
                    return Encoding.UTF8.GetString(chars.Slice(nodeWithPosition.PositionIndex, functionDeclarationNode.Length));
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
                    return Encoding.UTF8.GetString(chars.Slice(nodeWithPosition.PositionIndex, variableDeclarationNode.Length));
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
                    return Encoding.UTF8.GetString(chars.Slice(nodeWithPosition.PositionIndex, variableReferenceNode.Length));
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
