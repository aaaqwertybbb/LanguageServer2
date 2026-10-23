namespace JSLSApp.Syntax;

internal static class SyntaxHelper
{
    public static SyntaxNode? RecursiveSearch_BodyOnly(Scope scope, int indexLine, ref int totalChecks, ref int nodeCount)
    {
        nodeCount += scope.CountOfChildrenThatAreDefinitions;

        if (scope.CountOfChildrenThatAreDefinitions == 0)
        {
            return null;
        }

        foreach (var child_node in scope.BodyList)
        {
            if (child_node.Scope is null ||
                child_node.Scope.Type == BodyKind.EXTERNAL_FunctionBody ||
                (child_node.Scope.Start.line > indexLine || child_node.Scope.End.line < indexLine))
            {
                continue;
            }

            totalChecks++;
            if (child_node.Start.line == indexLine)
            {
                nodeCount += child_node.Scope.CountOfChildrenThatAreDefinitions;
                return child_node;
            }
            // super arbitrary 1,000 limit until I sleep on the logic.
            // it should do a tree walking -ish algorithm so 1k isn't too bad?
            if (totalChecks > 1000)
            {
                return null;
            }

            var nodeThatWasCloser = RecursiveSearch_BodyOnly(child_node.Scope, indexLine, ref totalChecks, ref nodeCount);
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

    public static SyntaxNode? RecursiveSearch(Scope scope, int indexLine, ref int totalChecks)
    {
        foreach (var child_node in scope.BodyList)
        {
            if (child_node.Scope is not null && child_node.Scope.Type == BodyKind.EXTERNAL_FunctionBody)
            {
                continue;
            }
            totalChecks++;
            if (child_node.Start.line == indexLine)
            {
                return child_node;
            }
            // super arbitrary 1,000 limit until I sleep on the logic.
            // it should do a tree walking -ish algorithm so 1k isn't too bad?
            if (totalChecks > 1000)
            {
                return null;
            }
            if (child_node.Scope is not null && child_node.Scope.End.line > indexLine)
            {
                if (child_node.Scope.BodyList.Count > 0)
                {
                    var node = RecursiveSearch(child_node.Scope, indexLine, ref totalChecks);
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
