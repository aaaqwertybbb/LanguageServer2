namespace JSLSApp.LspTypes;

/// <summary>
/// Obsolete
/// </summary>
[Obsolete("0 references?")]
public class TextDocumentDocumentSymbolResponseResult
{
    /// <summary>
    /// Is result the array or does result contain a single property of which is the array?
    /// </summary>
    public DocumentSymbol[] documentSymbols { get; set; }
}
