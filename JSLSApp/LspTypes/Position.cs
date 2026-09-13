namespace JSLSApp.LspTypes;

public struct Position(int line, int character)
{
    /**
	 * Line position in a document (zero-based).
	 * 
	 * This is a comment from myself not the docs:
	 *     TODO: consider uint because the docs specifically said 'uinteger'
	 */
    public int line { get; set; } = line;

    /**
	 * Character offset on a line in a document (zero-based). The meaning of this
	 * offset is determined by the negotiated `PositionEncodingKind`.
	 *
	 * If the character value is greater than the line length it defaults back
	 * to the line length.
	 * 
	 * This is a comment from myself not the docs:
	 *     TODO: consider uint because the docs specifically said 'uinteger'
	 */
    public int character { get; set; } = character;
}
