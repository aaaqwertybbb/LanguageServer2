namespace JSLSApp.LspTypes;

public class CustomEditorGetNodesAdjacentToCursorFromLspRequestParams
{
    public required TextDocumentIdentifier textDocument { get; set; }
    public Position position { get; set; }
}
