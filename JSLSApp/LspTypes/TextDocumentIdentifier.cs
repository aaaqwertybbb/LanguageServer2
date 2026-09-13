namespace JSLSApp.LspTypes;

public class TextDocumentIdentifier
{
    /**
	 * The text document's URI.
	 * 
	 * (myself): It is thought that null is valid for these because you could use it to specify some kind of scratch pad like text file? I have no idea though.
	 */
    public string? uri { get; set; }
}
