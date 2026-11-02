using JSLSApp.Syntax;

namespace JSLSApp;

public class JavaScriptDocument
{
    public JavaScriptDocument(string uriPath, byte[] chars, int capacity, int count, bool hadBomMark)
    {
        UriPath = uriPath;
        Chars = chars;
        Capacity = capacity;
        Count = count;
        // This is just a hack for now otherwise if statements and other not handled scopes would close the scope of the parent.
        CompilationUnit = new JavaScriptCompilationUnit(new(), new GlobalNode(0, 0), new());
        HadBomMark = hadBomMark;
    }

    public string UriPath { get; }
    public byte[] Chars { get; }
    public int Capacity { get; }
    public int Count { get; }
    public bool HasBeenParsedAtLeastOnce { get; set; }
    public JavaScriptCompilationUnit CompilationUnit { get; set; }
    public bool HadBomMark { get; set; }
}
