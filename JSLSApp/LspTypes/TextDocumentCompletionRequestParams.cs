namespace JSLSApp.LspTypes;

public class TextDocumentCompletionRequestParams
{
    public required TextDocumentIdentifier textDocument { get; set; }
    public Position position { get; set; }
}

public class TextDocumentCompletionRequestParams_slice
{
    public required TextDocumentIdentifier textDocument { get; set; }
    public int indexStart { get; set; }
    public int indexEnd { get; set; }
}
