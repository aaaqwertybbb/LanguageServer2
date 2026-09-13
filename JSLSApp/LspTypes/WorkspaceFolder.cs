namespace JSLSApp.LspTypes;

public class WorkspaceFolder
{
    // The associated URI for this workspace folder
    public string uri { get; set; }

	// The name of the workspace folder. Used to refer to this * workspace folder in the user interface.
	public string name { get; set; }
}
