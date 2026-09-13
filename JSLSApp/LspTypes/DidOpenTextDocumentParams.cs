namespace JSLSApp.LspTypes;

public class DidOpenTextDocumentParams
{
    /**
	 * The document that was opened.
	 */
    public required TextDocumentItem textDocument { get; set; }
}
