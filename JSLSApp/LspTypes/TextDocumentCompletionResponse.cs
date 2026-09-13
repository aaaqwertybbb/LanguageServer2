namespace JSLSApp.LspTypes;

public class TextDocumentCompletionResponse
{
    public TextDocumentCompletionResponse(int id, TextDocumentCompletionResponseResult result)
    {
        this.result = result;
		this.id = id;
    }

    public int id { get; }

    public TextDocumentCompletionResponseResult result { get; set; }
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
