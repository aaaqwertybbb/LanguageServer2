namespace JSLSApp.LspTypes;

public class TextDocumentHoverRequest
{
    public int id { get; set; }
    public string method { get; set; }
    public required TextDocumentHoverRequestParams @params { get; set; }
}
