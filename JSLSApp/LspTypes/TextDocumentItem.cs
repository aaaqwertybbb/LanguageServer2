namespace JSLSApp.LspTypes;

public class TextDocumentItem
{
    /**
	 * The text document's URI.
	 * 
	 * (myself): It is thought that null is valid for these because you could use it to specify some kind of scratch pad like text file? I have no idea though.
	 */
    public string? uri { get; set; }

    /**
	 * The text document's language identifier.
	 */
    public string? languageId { get; set; }

    /**
	 * The version number of this document (it will increase after each
	 * change, including undo/redo).
	 */
    public int version { get; set; }

    /**
	 * The content of the opened text document.
	 */
    public string? text { get; set; }
}
