namespace JSLSApp.LspTypes;

public class CustomEditorGetNodesAdjacentToCursorFromLspRequestResponse
{
    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponse()
    {
        
    }

    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponse(int id, CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResult customEditorGetNodesAdjacentToCursorFromLspRequestResponseResult)
    {
        this.id = id;
        result = customEditorGetNodesAdjacentToCursorFromLspRequestResponseResult;
    }

    public int id { get; set;  }

    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResult result { get; set; }
}
