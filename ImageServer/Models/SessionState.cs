namespace ImageServer.Models
{
    public enum SessionState
    {
        WaitingForCommand = 1,
        Ready = 2,
        SendingImage = 3,
        Closed = 4
    }
}