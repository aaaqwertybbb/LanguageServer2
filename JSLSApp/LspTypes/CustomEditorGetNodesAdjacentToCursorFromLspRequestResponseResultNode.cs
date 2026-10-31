namespace JSLSApp.LspTypes;

public class CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode
{
    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode()
    {
        
    }

    public CustomEditorGetNodesAdjacentToCursorFromLspRequestResponseResultNode(string syntaxKindString, string identifier, Position start)
    {
        SyntaxKindString = syntaxKindString;
        Identifier = identifier;
        Start = start;
    }

    public string SyntaxKindString { get; set; }
    public string Identifier { get; set; }
    public Position Start { get; set; }
}
