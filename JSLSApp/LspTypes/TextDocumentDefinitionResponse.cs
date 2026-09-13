namespace JSLSApp.LspTypes;

/*
// 2. Server answers with target file destination
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "uri": "file:///path/to/utils.ts",
    "range": {
      "start": { "line": 10, "character": 0 },
      "end": { "line": 15, "character": 1 }
    }
  }
}
*/

public class TextDocumentDefinitionResponse
{
    public TextDocumentDefinitionResponse(int id, string uri, Range? range)
    {
        this.result = new TextDocumentDefinitionResponseResult(uri, range);
		this.id = id;
    }

    public int id { get; }

    public TextDocumentDefinitionResponseResult result { get; set; }
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
