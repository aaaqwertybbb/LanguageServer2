namespace JSLSApp.LspTypes;

public class CustomEditorGetNodesAdjacentToCursorFromLspRequest
{
    public int id { get; set; }
    public string method { get; set; }
    public required CustomEditorGetNodesAdjacentToCursorFromLspRequestParams @params { get; set; }
}
