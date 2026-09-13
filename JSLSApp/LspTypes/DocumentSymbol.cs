namespace JSLSApp.LspTypes;

public class DocumentSymbol
{
    /**
	 * The name of this symbol. Will be displayed in the user interface and
	 * therefore must not be an empty string or a string only consisting of
	 * white spaces.
	 */
    public string name { get; set; }

    /**
	 * More detail for this symbol, e.g the signature of a function.
	 */
    public string detail { get; set; }

    /**
	 * The kind of this symbol.
	 */
    public SymbolKind kind { get; set; }

    /**
	 * Tags for this document symbol.
	 *
	 * @since 3.16.0
	 */
    public SymbolTag[]? tags { get; set; }

    /**
	 * Indicates if this symbol is deprecated.
	 *
	 * @deprecated Use tags instead
	 */
    public bool deprecated { get; set; }

    /**
	 * The range enclosing this symbol not including leading/trailing whitespace
	 * but everything else like comments. This information is typically used to
	 * determine if the clients cursor is inside the symbol to reveal it  in the
	 * UI.
	 */
    public Range range { get; set; }

    /**
	 * The range that should be selected and revealed when this symbol is being
	 * picked, e.g. the name of a function. Must be contained by the `range`.
	 */
    public Range selectionRange { get; set; }

    /**
	 * Children of this symbol, e.g. properties of a class.
	 */
    public DocumentSymbol[] children { get; set; }
}
