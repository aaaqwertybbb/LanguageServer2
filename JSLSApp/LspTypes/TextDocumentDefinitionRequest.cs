namespace JSLSApp.LspTypes;

public class TextDocumentDefinitionRequest
{
    public int id { get; set; }
    public string method { get; set; }
    public required TextDocumentDefinitionRequestParams @params { get; set; }
}
