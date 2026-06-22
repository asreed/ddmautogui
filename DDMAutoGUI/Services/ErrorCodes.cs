namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Contains standardized error codes and messages for the DDM Auto GUI application.
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>
        /// Represents a connection error to the controller.
        /// </summary>
        public static readonly ErrorInfo conCont = new ErrorInfo("E001", "Failed to connect to controller");

        /// <summary>
        /// Represents an error verifying settings on the controller.
        /// </summary>
        public static readonly ErrorInfo conSettings = new ErrorInfo("E002", "Failed to verify settings on controller");

        /// <summary>
        /// Represents a connection error to the I/O-Link master.
        /// </summary>
        public static readonly ErrorInfo conIOMaster = new ErrorInfo("E003", "Failed to connect to I/O-Link master");

        /// <summary>
        /// Represents a connection error to I/O-Link port 1.
        /// </summary>
        public static readonly ErrorInfo conIO1 = new ErrorInfo("E004", "Failed to connect to I/O-Link port 1");

        /// <summary>
        /// Represents a connection error to I/O-Link port 2.
        /// </summary>
        public static readonly ErrorInfo conIO2 = new ErrorInfo("E005", "Failed to connect to I/O-Link port 2");

        /// <summary>
        /// Represents a connection error to I/O-Link port 3.
        /// </summary>
        public static readonly ErrorInfo conIO3 = new ErrorInfo("E006", "Failed to connect to I/O-Link port 3");

        /// <summary>
        /// Represents a connection error to I/O-Link port 4.
        /// </summary>
        public static readonly ErrorInfo conIO4 = new ErrorInfo("E007", "Failed to connect to I/O-Link port 4");

        /// <summary>
        /// Represents a connection error to the laser sensor.
        /// </summary>
        public static readonly ErrorInfo conLaser = new ErrorInfo("E008", "Failed to connect to laser sensor");

        /// <summary>
        /// Represents a connection error to the top camera.
        /// </summary>
        public static readonly ErrorInfo conCamTop = new ErrorInfo("E009", "Failed to connect to top camera");

        /// <summary>
        /// Represents a connection error to the side camera.
        /// </summary>
        public static readonly ErrorInfo conCamSide = new ErrorInfo("E010", "Failed to connect to side camera");

        /// <summary>
        /// Encapsulates error code and message information.
        /// </summary>
        public class ErrorInfo
        {
            /// <summary>
            /// Gets the error code identifier.
            /// </summary>
            public string Code { get; }

            /// <summary>
            /// Gets the error message.
            /// </summary>
            public string Message { get; }

            /// <summary>
            /// Initializes a new instance of the ErrorInfo class.
            /// </summary>
            /// <param name="code">The error code identifier.</param>
            /// <param name="message">The error message.</param>
            public ErrorInfo(string code, string message)
            {
                Code = code;
                Message = message;
            }

            /// <summary>
            /// Gets the error code (backward compatibility).
            /// </summary>
            public string code => Code;

            /// <summary>
            /// Gets the error message (backward compatibility).
            /// </summary>
            public string msg => Message;
        }
    }
}