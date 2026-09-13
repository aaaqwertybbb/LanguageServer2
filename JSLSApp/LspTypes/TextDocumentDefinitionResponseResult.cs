namespace JSLSApp.LspTypes;

public class TextDocumentDefinitionResponseResult
{
    public TextDocumentDefinitionResponseResult(string uri, Range? range)
    {
        this.uri = uri;
        this.range = range;
    }

    public string uri { get; }
    public Range? range { get; }
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
