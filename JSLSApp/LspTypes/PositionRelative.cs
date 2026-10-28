namespace JSLSApp.LspTypes;

/// <summary>
/// I'm not sure if tracking a relative line and character makes sense or not.
/// I feel far more comfortable with the idea of a relative position and then as I move through the AST I track the current line/column.
/// </summary>
public struct PositionRelative(int line, int character, int position)
{
    /**
	 * Line position in a document (zero-based).
	 * 
	 * This is a comment from myself not the docs:
	 *     TODO: consider uint because the docs specifically said 'uinteger'
	 *     
	 *  Line should reset column?
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

	public int position { get; set; } = position;
}
