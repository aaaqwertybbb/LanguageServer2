namespace JSLSApp.LspTypes;

/*
{
    "method": "initialize",
    "id": 0,
    "params": {
        "processId": 31892,
        "clientInfo": {
            "name": "TextEditor123",
            "version": "0.0.1"
        },
        "rootUri": "C:\\Users\\hunte\\Repos\\JavaScript"
    }
}
 */

/// <summary>
/// TODO: Replicate the typescipt interfaces that the protocol provides.
/// </summary>
public class Message
{
    public string? Method { get; set; }
}
