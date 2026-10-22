namespace JSLSApp.LspTypes;

public class WindowShowMessageNotification(WindowShowMessageNotificationParams @params)
{
    public string? method { get; set; } = "window/showMessage";
    public WindowShowMessageNotificationParams @params { get; set; } = @params;
}
