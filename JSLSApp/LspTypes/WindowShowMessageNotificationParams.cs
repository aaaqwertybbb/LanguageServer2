namespace JSLSApp.LspTypes;

public class WindowShowMessageNotificationParams(int type, string message)
{
    public int type { get; set; } = type;
    public string message { get; set; } = message;
}
