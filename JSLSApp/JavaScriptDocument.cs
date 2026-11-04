using JSLSApp.Syntax;

namespace JSLSApp;

public class JavaScriptDocument
{
    public JavaScriptDocument(string uriPath, byte[] chars, int capacity, int count, bool hadBomMark, byte lineEndingKind)
    {
        UriPath = uriPath;
        Chars = chars;
        Capacity = capacity;
        Count = count;
        // This is just a hack for now otherwise if statements and other not handled scopes would close the scope of the parent.
        CompilationUnit = new JavaScriptCompilationUnit(new(), new GlobalNode(0, 0), new());
        HadBomMark = hadBomMark;
        LineEndingKind = lineEndingKind;
    }

    public string UriPath { get; }
    public byte[] Chars { get; set; }
    public int Capacity { get; set; }
    public int Count { get; set; }
    public bool HasBeenParsedAtLeastOnce { get; set; }
    public JavaScriptCompilationUnit CompilationUnit { get; set; }
    public bool HadBomMark { get; set; }
    /// <summary>
    /// 0 => None
    /// 1 => CR
    /// 2 => LF
    /// 3 => CRLF
    /// </summary>
    public byte LineEndingKind { get; set; }
}
