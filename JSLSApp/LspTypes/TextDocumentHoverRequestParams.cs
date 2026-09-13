namespace JSLSApp.LspTypes;

public class TextDocumentHoverRequestParams
{
    public required TextDocumentIdentifier textDocument { get; set; }
    public Position position { get; set; }
}
