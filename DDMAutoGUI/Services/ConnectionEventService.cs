namespace DDMAutoGUI.Services
{
    public interface IConnectionEventService
    {
        event EventHandler ControllerConnected;
        event EventHandler ControllerDisconnected;
        bool IsConnected { get; }
        string ConnectedIP { get; }
    }

    public class ConnectionEventService : IConnectionEventService
    {
        public event EventHandler ControllerConnected;
        public event EventHandler ControllerDisconnected;

        public bool IsConnected { get; private set; }
        public string ConnectedIP { get; private set; }

        public void NotifyConnected(string ip)
        {
            IsConnected = true;
            ConnectedIP = ip;
            ControllerConnected?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyDisconnected()
        {
            IsConnected = false;
            ConnectedIP = string.Empty;
            ControllerDisconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}