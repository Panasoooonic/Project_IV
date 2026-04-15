using ImageServer.Models;

namespace ImageServer.Managers
{
    /// <summary>
/// Manages the current state of a client session.
/// </summary>
/// <remarks>
/// Ensures only valid operations occur based on the current state.
/// </remarks>
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

/// <summary>
/// Determines if an image request is allowed.
/// </summary>
/// <param name="isAuthenticated">Authentication status</param>
/// <returns>True if allowed</returns>
        public bool CanRequestImage(bool isAuthenticated)
        {
            return isAuthenticated && CurrentState == SessionState.Ready;
        }
    }
}