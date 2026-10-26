using JSLSApp.Syntax;

namespace JSLSApp;

public class JavaScriptDocument
{
    public JavaScriptDocument(string uriPath, List<char> chars)
    {
        UriPath = uriPath;
        Chars = chars;
        // This is just a hack for now otherwise if statements and other not handled scopes would close the scope of the parent.
        CompilationUnit = new JavaScriptCompilationUnit(new(), new GlobalNode(), new());
    }

    public string UriPath { get; }
    public List<char> Chars { get; }
    public bool HasBeenParsedAtLeastOnce { get; set; }
    public JavaScriptCompilationUnit CompilationUnit { get; set; }
}
