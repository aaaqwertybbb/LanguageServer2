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
    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode(SyntaxKind syntaxKind, string identifier, Position start)
    {
        SyntaxKind = syntaxKind;
        Identifier = identifier;
        Start = start;
    }

    public SyntaxKind SyntaxKind { get; set; }
    public string Identifier { get; set; }
    public Position Start { get; set; }
}
