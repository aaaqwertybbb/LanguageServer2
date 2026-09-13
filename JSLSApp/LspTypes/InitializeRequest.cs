namespace JSLSApp.LspTypes;

/// <summary>
/// TODO: Replicate the typescipt interfaces that the protocol provides.
/// </summary>
public class InitializeRequest
{
    public string? Method { get; set; }
    public int Id { get; set; }
    public required InitializeRequestParams @params { get; set; }
}
