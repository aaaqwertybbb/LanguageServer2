namespace JSLSApp.LspTypes;

public class CustomEditorVerifyLspTextAgainstEditorTextRequest
{
    public int id { get; set; }
    public string method { get; set; }
    public required TextDocumentDocumentSymbolRequestParams @params { get; set; }
}

public class CustomEditorVerifyLspTextAgainstEditorTextResponse
{
    /// <summary>
    /// result is: The text that the language server thinks it is.
    /// </summary>
    public CustomEditorVerifyLspTextAgainstEditorTextResponse(int id, string result)
    {
        this.id = id;
        this.result = result;
    }
    public int id { get; set; }
    /// <summary>
    /// The text that the language server thinks it is.
    /// </summary>
    public string result { get; set; }
}
