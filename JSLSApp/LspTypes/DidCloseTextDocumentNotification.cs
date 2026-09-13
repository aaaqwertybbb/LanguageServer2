namespace JSLSApp.LspTypes;

public class DidCloseTextDocumentNotification
{
    public string? method { get; set; }
    public required DidCloseTextDocumentParams @params { get; set; }

}

public class DidChangeTextDocumentNotification
{
    public string? method { get; set; }
    public required DidChangeTextDocumentParams @params { get; set; }

}

public class DidChangeTextDocumentParams
{
    /**
	 * The document that did change. The version number points
	 * to the version after all provided content changes have
	 * been applied.
	 */
    public required VersionedTextDocumentIdentifier textDocument { get; set; }

    /**
	 * The actual content changes. The content changes describe single state
	 * changes to the document. So if there are two content changes c1 (at
	 * array index 0) and c2 (at array index 1) for a document in state S then
	 * c1 moves the document from S to S' and c2 from S' to S''. So c1 is
	 * computed on the state S and c2 is computed on the state S'.
	 *
	 * To mirror the content of a document using change events use the following
	 * approach:
	 * - start with the same initial content
	 * - apply the 'textDocument/didChange' notifications in the order you
	 *   receive them.
	 * - apply the `TextDocumentContentChangeEvent`s in a single notification
	 *   in the order you receive them.
	 */
    public required TextDocumentContentChangeEvent[] contentChanges { get; set; }
}

public class VersionedTextDocumentIdentifier
{
    /**
	 * The version number of this document.
	 *
	 * The version number of a document will increase after each change,
	 * including undo/redo. The number doesn't need to be consecutive.
	 */
    public int version { get; set; }

    /**
     * The text document's URI.
     * 
     * (myself): It is thought that null is valid for these because you could use it to specify some kind of scratch pad like text file? I have no idea though.
     */
    public string? uri { get; set; }
}

/**
 * An event describing a change to a text document. If only a text is provided
 * it is considered to be the full content of the document.
 */
public class TextDocumentContentChangeEvent
{
	/**
	 * The range of the document that changed.
	 */
	public Range? range { get; set; }

	/**
	 * The optional length of the range that got replaced.
	 *
	 * @deprecated use range instead.
	 */
	public uint? rangeLength { get; set; }

    /**
	 * The new text for the provided range.
	 * 
	 * OR
	 * 
	 * The new text of the whole document.
	 */
    public string text { get; set; }
}
