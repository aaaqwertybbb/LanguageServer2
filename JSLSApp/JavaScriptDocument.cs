namespace JSLSApp;

public class JavaScriptDocument
{
    public JavaScriptDocument(string uriPath, List<char> chars)
    {
        UriPath = uriPath;
        Chars = chars;
    }

    public string UriPath { get; }
    public List<char> Chars { get; }
    public bool HasBeenParsedAtLeastOnce { get; set; }
    public JavaScriptCompilationUnit CompilationUnit { get; set; } = new JavaScriptCompilationUnit(new(), new(), new());
}
