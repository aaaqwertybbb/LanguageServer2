namespace JSLSApp.LspTypes;

public class TextDocumentCompletionRequest
{
    public int id { get; set; }
    public string method { get; set; }
    public required TextDocumentCompletionRequestParams @params { get; set; }
}

public class TextDocumentCompletionRequest_slice
{
    public int id { get; set; }
    public string method { get; set; }
    public required TextDocumentCompletionRequestParams_slice @params { get; set; }
}
