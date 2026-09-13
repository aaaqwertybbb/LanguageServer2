namespace JSLSApp.LspTypes;

public class TextDocumentDocumentSymbolRequest
{
    public int id { get; set; }
    public string method { get; set; }
    public required TextDocumentDocumentSymbolRequestParams @params { get; set; }
}
