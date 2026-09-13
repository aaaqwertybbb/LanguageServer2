namespace JSLSApp.LspTypes;

public class TextDocumentCompletionResponseResult
{
    public bool isIncomplete { get; set; }
    public TextDocumentCompletionItem[] items { get; set; }
    /// <summary>
    /// I don't think this is in LSP specification but I need to start like this cause it is only way I'll get something "initially working".
    /// </summary>
    public int itemsStart { get; set; }
    /// <summary>
    /// I don't think this is in LSP specification but I need to start like this cause it is only way I'll get something "initially working".
    /// </summary>
    public int itemsEnd { get; set; }
    /// <summary>
	/// I don't think this is in LSP specification but I need to start like this cause it is only way I'll get something "initially working".
	/// </summary>
    public int totalLength { get; set; }
}

/*
 interface ResponseMessage extends Message {
		// The request id.
		id: integer | string | null;

		// The result of a request. This member is REQUIRED on success.* This member MUST NOT exist if there was an error invoking the method.
		result?: LSPAny;

		// The error object in case a request fails.
		error?: ResponseError;
	}
 */
