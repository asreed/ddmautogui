namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Contains standardized error codes and messages for the DDM Auto GUI application.
    /// </summary>
    public static class ErrorCodes
    {

        public static readonly ErrorInfo conCont =      new ErrorInfo("-5000", "Failed to connect to controller");
        public static readonly ErrorInfo conSettings =  new ErrorInfo("-5001", "Failed to verify settings on controller");
        public static readonly ErrorInfo conIOMaster =  new ErrorInfo("-5002", "Failed to connect to I/O-Link master");
        public static readonly ErrorInfo conIO1 =       new ErrorInfo("-5003", "Failed to connect to I/O-Link port 1");
        public static readonly ErrorInfo conIO2 =       new ErrorInfo("-5004", "Failed to connect to I/O-Link port 2");
        public static readonly ErrorInfo conIO3 =       new ErrorInfo("-5005", "Failed to connect to I/O-Link port 3");
        public static readonly ErrorInfo conIO4 =       new ErrorInfo("-5006", "Failed to connect to I/O-Link port 4");
        public static readonly ErrorInfo conLaser =     new ErrorInfo("-5007", "Failed to connect to laser sensor");
        public static readonly ErrorInfo conCamTop =    new ErrorInfo("-5008", "Failed to connect to top camera");
        public static readonly ErrorInfo conCamSide =   new ErrorInfo("-5009", "Failed to connect to side camera");

        public class ErrorInfo
        {

            public string Code { get; }
            public string Message { get; }

            public ErrorInfo(string code, string message)
            {
                Code = code;
                Message = message;
            }

            public string code => Code;
            public string msg => Message;
        }
    }
}
