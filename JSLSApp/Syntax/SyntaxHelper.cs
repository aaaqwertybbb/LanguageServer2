namespace JSLSApp.Syntax;

internal static class SyntaxHelper
{
    public static SyntaxNode? RecursiveSearch_BodyOnly(SyntaxNode scopeProvider, int indexLine, ref int totalChecks, ref int nodeCount)
    {
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
            if (child_node.Start.line == indexLine)
            {
                nodeCount += childScope.CountOfChildrenThatAreDefinitions;
                return child_node;
            }
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

    public static SyntaxNode? RecursiveSearch(SyntaxNode scopeProvider, int indexLine, int indexCharacter, ref int totalChecks)
    {
        ref var scope = ref scopeProvider.GetScope();

        for (int i = 0; i < scope.BodyList.Count; i++)
        {
            SyntaxNode? child_node = scope.BodyList[i];

            if (child_node.IsScopeProvider && child_node.GetScope().Type == BodyKind.EXTERNAL_FunctionBody)
            {
                continue;
            }
            totalChecks++;
            if (child_node.Start.line == indexLine)
            {
                // TODO: This might be inaccurate for a function definition (or etc...) that spans a single line; if you tried to target an identifier in the code block it might target the function definition I'm not sure.
                if (child_node.Start.character <= indexCharacter && child_node.End.character >= indexCharacter)
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
            if (childScope.End.line > indexLine)
            {
                if (childScope.BodyList.Count > 0)
                {
                    var node = RecursiveSearch(child_node, indexLine, indexCharacter, ref totalChecks);
                    if (node is not null)
                    {
                        return node;
                    }
                }
                break;
            }
        }
        return null;
    }
}
