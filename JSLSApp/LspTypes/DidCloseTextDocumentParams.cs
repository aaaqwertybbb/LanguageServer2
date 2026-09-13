namespace JSLSApp.LspTypes;

public class DidCloseTextDocumentParams
{
    /**
	 * The document that was closed.
	 */
    public TextDocumentIdentifier textDocument { get; set; }
}
