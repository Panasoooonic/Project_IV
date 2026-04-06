namespace ImageServer.Models
{
    public enum SessionState
    {
        WaitingForConnection = 1,
        Connected = 2,
        Authenticated = 3,
        Ready = 4,
        SendingImage = 5,
        Closed = 6
    }
}