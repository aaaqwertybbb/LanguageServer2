using JSLSApp.LspTypes;

namespace JSLSApp;

public class FunctionDefinitionSyntax
{
    public FunctionDefinitionSyntax(Position startPosition, string name = "unknown")
    {
        StartPosition = startPosition;
        Name = name;
    }

    public Position StartPosition { get; set; }
    public string Name { get; set; }
}
