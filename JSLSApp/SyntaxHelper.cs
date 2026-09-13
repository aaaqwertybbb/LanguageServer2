namespace JSLSApp;

internal static class SyntaxHelper
{
    public static SyntaxNode? RecursiveSearch_BodyOnly(List<SyntaxNode> bodyList, Dictionary<string, SyntaxNode> lexicalScope, int indexLine, ref int totalChecks, ref int nodeCount)
    {
        // TODO: This misses the count of the global scope if only the global scope is checked...
        // ...I'm gonna add it as a line immediately following this invocation as a hack for now. (1 of 2)
        foreach (var child_node in bodyList)
        {
            if (child_node.Body is null ||
                child_node.Body.Type == BodyKind.EXTERNAL_FunctionBody ||
                (child_node.Body.Start.line > indexLine || child_node.Body.End.line < indexLine))
            {
                continue;
            }

            totalChecks++;
            if (child_node.Start.line == indexLine)
            {
                nodeCount += lexicalScope.Count; // awkward but believed necessary (1 of 2)
                return child_node;
            }
            // super arbitrary 1,000 limit until I sleep on the logic.
            // it should do a tree walking -ish algorithm so 1k isn't too bad?
            if (totalChecks > 1000)
            {
                return null;
            }
            nodeCount += lexicalScope.Count; // awkward but believed necessary (2 of 2)

            var nodeThatWasCloser = RecursiveSearch_BodyOnly(child_node.Body.BodyList, child_node.Body.Scope.LexicalScope, indexLine, ref totalChecks, ref nodeCount);
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

    public static SyntaxNode? RecursiveSearch(List<SyntaxNode> bodyList, int indexLine, ref int totalChecks)
    {
        foreach (var child_node in bodyList)
        {
            if (child_node.Body is not null && child_node.Body.Type == BodyKind.EXTERNAL_FunctionBody)
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
            if (child_node.Body is not null && child_node.Body.End.line > indexLine)
            {
                if (child_node.Body.BodyList.Count > 0)
                {
                    var node = RecursiveSearch(child_node.Body.BodyList, indexLine, ref totalChecks);
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
