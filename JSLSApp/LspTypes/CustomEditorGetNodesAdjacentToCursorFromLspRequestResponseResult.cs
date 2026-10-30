using JSLSApp.Syntax;

namespace JSLSApp.LspTypes;

public class CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResult
{
    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResult(CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode before, CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode among, CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode after)
    {
        Before = before;
        Among = among;
        After = after;
    }

    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode Before { get; set; }
    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode Among { get; set; }
    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode After { get; set; }
}

public class CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode
{
    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode(string syntaxKindString, string identifier, Position start)
    {
        SyntaxKindString = syntaxKindString;
        Identifier = identifier;
        Start = start;
    }

    public string SyntaxKindString { get; set; }
    public string Identifier { get; set; }
    public Position Start { get; set; }
}
