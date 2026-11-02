namespace JSLSApp.LspTypes;

public class CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode
{
    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode()
    {
        
    }

    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode(string syntaxKindString, string identifier, int positionIndex)
    {
        SyntaxKindString = syntaxKindString;
        Identifier = identifier;
        PositionIndex = positionIndex;
    }

    public string SyntaxKindString { get; set; }
    public string Identifier { get; set; }
    public int PositionIndex { get; set; }
}
