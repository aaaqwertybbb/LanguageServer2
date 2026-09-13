namespace JSLSApp.LspTypes;

public class DidOpenTextDocumentNotification
{
    public string? method { get; set; }
    public required DidOpenTextDocumentParams @params { get; set; }

}
