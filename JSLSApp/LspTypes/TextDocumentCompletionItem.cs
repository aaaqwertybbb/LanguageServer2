namespace JSLSApp.LspTypes;

public class TextDocumentCompletionItem
{
    public string label { get; set; }
    public int kind { get; set; }
    public string detail { get; set; }
    public string insertText { get; set; }
    public string documentation { get; set; }
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
