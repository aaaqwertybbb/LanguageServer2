namespace JSLSApp.LspTypes;

public class TextDocumentDefinitionRequestParams
{
    public required TextDocumentIdentifier textDocument { get; set; }
    public Position position { get; set; }
}
