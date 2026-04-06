using ImageServer.Models;

namespace ImageServer.Managers
{
    public class StateManager
    {
        public SessionState CurrentState { get; private set; } = SessionState.WaitingForConnection;

        public void MarkConnected()
        {
            CurrentState = SessionState.Connected;
        }

        public void MarkAuthenticated()
        {
            CurrentState = SessionState.Authenticated;
        }

        public void MarkReady()
        {
            CurrentState = SessionState.Ready;
        }

        public void MarkSendingImage()
        {
            CurrentState = SessionState.SendingImage;
        }

        public void MarkClosed()
        {
            CurrentState = SessionState.Closed;
        }

        public bool CanAuthenticate()
        {
            return CurrentState == SessionState.Connected;
        }

        public bool CanRequestImage(bool isAuthenticated)
        {
            return isAuthenticated && CurrentState == SessionState.Ready;
        }
    }
}