using JSLSApp.Syntax;

namespace JSLSApp.LspTypes;

public class CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResult
{
    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResult()
    {
        
    }

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
