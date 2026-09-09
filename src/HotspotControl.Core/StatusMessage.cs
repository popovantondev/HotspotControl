namespace HotspotControl.Core;

public sealed class StatusMessage
{
    private string? operationMessage;
    public void SetOperation(string message) => operationMessage = message;
    public void ClearOperation() => operationMessage = null;
    public string ForRefresh(string statusMessage) => operationMessage ?? statusMessage;
}
