namespace JSLSApp.LspTypes;

/// <summary>
/// TODO: If LSP has this request then use their definition...
/// ...
/// Explanation: The Editor Client performs a global context lex of the entire text
/// to know where the:
/// - multiline comments
/// - multiline strings
/// are.
/// 
/// The syntax highlighting of the editor is two steps.
/// - global context
/// - line by line as you scroll them into view
/// 
/// The "line by line as you scroll them into view"
/// lex only knows about the text on that line.
/// But it references the global context to understand whether the line is encompassed by a multiline comment or string.
/// In which case, the multiline syntax would take precedence and be the context underwhich the line's text is understood.
/// </summary>
public class CustomFullFileLexRequest
{
    public int id { get; set; }
    public string method { get; set; }
    public required TextDocumentDocumentSymbolRequestParams @params { get; set; }
}
