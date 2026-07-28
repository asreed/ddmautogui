namespace DDMAutoGUI.Constants
{
    /// <summary>
    /// Standard user-facing text for the connection status field shown in the main
    /// window status bar. Centralized so the ViewModel and services surface identical
    /// wording for each connection state.
    /// </summary>
    public static class ConnectionStatusText
    {
        public const string NotConnected = "Not connected";
        public const string Connecting = "Connecting...";
        public const string Connected = "Connected";
        public const string ConnectionFailed = "Connection failed";
        public const string Disconnecting = "Disconnecting...";
        public const string Disconnected = "Disconnected";

        /// <summary>Connected status including the controller IP, e.g. "Connected (192.168.0.1)".</summary>
        public static string ConnectedTo(string ip) => $"Connected ({ip})";

        /// <summary>Error status with the failure detail appended.</summary>
        public static string Error(string detail) => $"Error: {detail}";
    }
}