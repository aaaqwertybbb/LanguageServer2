namespace JSLSApp.LspTypes;

/// <summary>
/// TODO: Replicate the typescipt interfaces that the protocol provides.
/// </summary>
public class InitializeRequestParams
{
    public int ProcessId { get; set; }
    public InitializeRequestParams_clientInfo? ClientInfo { get; set; }
    public string? rootUri { get; set; }
    public List<WorkspaceFolder>? workspaceFolders { get; set; }
}
